# Kiểm chứng backend Phase 3

Ngày thực hiện: 16/10/2026. Backend đã có API/service cho UC01–UC28. Chưa xác nhận hoàn tất trên SQL Server thật; giao diện Web/WinForms và kiểm thử tích hợp toàn sản phẩm thuộc các phase sau.

## Kết quả đã chạy

| Kiểm tra | Kết quả |
|---|---|
| Build toàn solution, gồm Web/Desktop/API và hai project test | Thành công, 0 warning, 0 error |
| Hospital.UnitTests | 8 passed |
| Hospital.IntegrationTests: database quan hệ và HTTP | 50 passed |
| Test SQL Server | 4 skipped vì thiếu HOSPITAL_TEST_SQLSERVER |
| Tổng | 58 passed, 0 failed, 4 skipped |
| EF migrations has-pending-model-changes | Không có thay đổi model chưa tạo migration |
| Script nâng cấp Phase 2 → Phase 3 | Đã sinh script idempotent; chưa thực thi trên SQL Server |

Môi trường: Windows, SDK .NET 10.0.402, runtime 10.0.13; giữ target net9.0 và dùng `DOTNET_ROLL_FORWARD=Major`. Test quan hệ chạy SQLite trên file database riêng cho mỗi test, có foreign key, transaction và unique index. Adapter test sinh concurrency token vì SQLite không sinh rowversion như SQL Server. HTTP dùng ASP.NET Core TestServer qua WebApplicationFactory, xác thực JWT thật; không bỏ qua middleware phân quyền.

Lệnh đã chạy ở thư mục gốc:

```powershell
$env:DOTNET_ROLL_FORWARD = 'Major'
dotnet build codes/HospitalAppointmentSystem.sln --no-restore --verbosity minimal
dotnet test codes/HospitalAppointmentSystem.sln --no-build --no-restore --verbosity minimal --logger 'trx;LogFileName=phase3.trx'
```

TRX nằm dưới `TestResults/` của mỗi project test, được gitignore vì là kết quả sinh tự động. Các file nguồn kiểm thử được lưu trong repository.

## Đối chiếu 18 tình huống bắt buộc

| # | Tình huống | Bằng chứng trong Hospital.IntegrationTests |
|---|---|---|
| 1 | Trùng username/email | ApiTests.AuthRejectsDuplicatesWrongPasswordInactiveAccountAndInvalidInput |
| 2 | Sai mật khẩu/tài khoản khóa | Cùng test auth; API trả 401 |
| 3 | Sai vai trò | ApiTests.WrongRoleIsForbidden, 5 trường hợp |
| 4 | Xem/sửa lịch của bệnh nhân khác | ApiTests.PatientOwnershipAndPaginationAreCheckedAtApi; AppointmentTests.PatientCannotBookForOrReadOrModifyAnotherPatient |
| 5 | Slot đã có người đặt | AppointmentTests.RejectsOccupiedSlotAndPatientOverlapButAllowsAdjacentSlots |
| 6 | Hai bệnh nhân đặt đồng thời | ConcurrencyTests.ConcurrentBookingsOnRelationalDatabaseHaveExactlyOneWinner đã qua trên SQLite; test SQL Server tương ứng đang Skip |
| 7 | Bệnh nhân có hai lịch chồng lấn | Test occupied/overlap; có kiểm tra hai khoảng tiếp giáp vẫn hợp lệ |
| 8 | Đổi lịch còn dưới 2 giờ | PatientRescheduleEnforcesExactTwoHourBoundary, cả 1.99 giờ và đúng 2 giờ |
| 9 | Đổi lịch thất bại giữ lịch cũ | FailedRescheduleKeepsOldAppointmentSlotAndHistory; RescheduleSaveFailureRollsBackBothSlotsAndHistory |
| 10 | Hủy giải phóng slot, giữ lịch sử | CancellationReleasesSlotAndPreservesHistoryForRebooking; đặt lại slot bằng bệnh nhân khác thành công |
| 11 | Tiếp nhận lịch không hợp lệ | ReceptionOnlyAcceptsValidSameDayAppointmentAndIsIdempotent; CancelledAppointmentCannotBeReceivedOrMarkedNoShow |
| 12 | Ghi vắng khi chưa hết giờ | NoShowRequiresEndTimeStrictlyPassed |
| 13 | Đóng slot đang Confirmed | AdminAndScheduleTests.CannotCloseOrChangeScheduleWithConfirmedAppointments |
| 14 | Ngừng bác sĩ/chuyên khoa và giữ lịch sử | InactivationWithFutureAppointmentsIsBlockedThenKeepsHistoryAfterCancellation; ScheduleChangeRetainsCancelledSlotsAndTheirAppointmentHistory |
| 15 | Vô hiệu Admin cuối cùng | LastActiveAdminCannotBeDisabledOrDemoted; test cạnh tranh trên SQL Server đang Skip |
| 16 | Gửi lại yêu cầu đặt | BookingIsAtomicAndIdempotentWithHistoryAuditAndNotification; API BookingThroughApiRequiresKeyAndVersionAndSupportsReplay |
| 17 | Database lỗi/lưu thất bại | ApiTests.DatabaseOutageReportsTraceAndLeavesNoPartialBooking; SaveFailureRollsBackSlotAppointmentHistoryAndOutbox; test lỗi kết nối thật với SQL Server chưa chạy |
| 18 | Thông báo sau đặt/đổi/hủy | AppointmentTests và NotificationTests; có kiểm tra lỗi ghi thông báo, retry không trùng, đổi lịch hủy lời nhắc cũ và ngoại lệ đặt sát giờ |

Các test khác xác minh mã định danh, phân trang, lịch 30 phút, giới hạn vai trò bác sĩ, thay đổi TokenVersion, rate limit, giới hạn dữ liệu danh mục và stale RowVersion.

## Luồng HTTP đã xác minh

`ApiTests.FullBackendFlowAdminToPatientDoctorAndReceptionUsesOneDatabase` thực hiện qua HTTP:

1. Admin tạo hồ sơ bác sĩ có chuyên khoa rồi cấp tài khoản bác sĩ.
2. Admin tạo ca trực, backend sinh hai slot 30 phút.
3. Patient tra cứu slot và đặt lịch.
4. Doctor đăng nhập, xem lịch vừa tạo trong phạm vi của mình.
5. Staff tiếp nhận khi đến ngày khám.
6. Patient đọc lại trạng thái CheckedIn và thấy thông báo của lịch.

Luồng này chạy trên một database SQLite thử nghiệm, không phải demo Web/WinForms hoặc xác minh SQL Server.

## Phần cần chạy trên SQL Server

Không tìm thấy service SQL Server và Docker engine chưa chạy trong môi trường thực hiện. Vì vậy chưa áp dụng migration lên HospitalDb, chưa xác minh rowversion và khóa Serializable trên SQL Server, chưa chạy API với SQL Server thật.

Người dùng đã chọn cấu hình SQL Server sau. Phần kiểm chứng này được để lại rõ ràng; không cài thêm hoặc thay đổi dịch vụ database trên máy trong lượt thực hiện này.

Bốn test đã chuẩn bị trong `SqlServerConcurrencyTests`:

- Chạy migrations và hai bệnh nhân tranh cùng slot.
- Gửi cùng RequestKey đồng thời phải trả cùng một lịch.
- Một bệnh nhân đặt đồng thời hai slot chồng lấn thuộc hai bác sĩ.
- Hai thao tác khóa Admin đồng thời phải giữ lại ít nhất một Admin hoạt động.

Theo hướng dẫn trong `api-documentation.md`, đặt HOSPITAL_TEST_SQLSERVER tới SQL Server thử nghiệm rồi chạy nhóm test này. Fixture tạo database `HospitalPhase3Tests_<GUID>`, kiểm tra tiền tố trước khi xóa; không dùng hoặc xóa HospitalDb.

Sau khi 4 test này qua, cần chạy API bình thường với SQL Server và kiểm tra Swagger/readiness trước khi xác nhận Phase 3 hoàn tất trên môi trường đích. Phase 5–7 vẫn cần giao diện và tích hợp Web/Desktop thực tế.
