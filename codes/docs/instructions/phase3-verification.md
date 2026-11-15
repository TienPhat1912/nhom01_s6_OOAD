# Kiểm chứng backend Phase 3

Ngày xác nhận: 17/10/2026. Phase 3 backend hoàn tất trên SQL Server thật, có API/service cho UC01–UC28. Giao diện Web/WinForms và kiểm thử tích hợp toàn sản phẩm thuộc các phase sau.

## Kết quả đã chạy

| Kiểm tra | Kết quả |
|---|---|
| Build toàn solution, gồm Web/Desktop/API và hai project test | Thành công, 0 warning, 0 error |
| Hospital.UnitTests | 8 passed |
| Hospital.IntegrationTests: SQLite và HTTP | 50 passed |
| Hospital.IntegrationTests: SQL Server | 7 passed (4 cạnh tranh, 3 backend) |
| Tổng | 65 passed, 0 failed, 0 skipped |
| EF migrations has-pending-model-changes | Không có thay đổi model chưa tạo migration |
| Migration Phase 2 → Phase 3 trên HospitalDb | Thành công, giữ 6 tài khoản và 2 lịch hẹn |
| Script phase3-upgrade.sql | Thực thi lại 2 lần sau migration, không lỗi và không tạo trùng |
| API chạy bình thường với HospitalDb | Root, readiness, Swagger, đăng nhập và đọc dữ liệu của 4 vai trò đều trả 200 |

Môi trường: Windows, SDK .NET 10.0.400, runtime .NET/ASP.NET Core 9.0.1, SQL Server 2022 Express 16.0.1000.6 trên `.\SQLEXPRESS`. Giữ target net9.0 và chạy bằng runtime 9, không cần `DOTNET_ROLL_FORWARD`.

Các test SQLite dùng file database riêng, foreign key, transaction và unique index; adapter tự sinh concurrency token. Bảy test SQL Server dùng database riêng, áp dụng migrations thật và rowversion do SQL Server sinh. HTTP dùng ASP.NET Core TestServer qua WebApplicationFactory, xác thực JWT thật và toàn bộ middleware phân quyền. Ngoài bộ test, API được khởi động qua Kestrel với cấu hình development bình thường để kiểm tra `HospitalDb`.

Chạy lại lệnh tại thư mục `codes`, nơi chứa solution:

```powershell
$env:HOSPITAL_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet build HospitalAppointmentSystem.sln --no-restore --verbosity minimal
dotnet test HospitalAppointmentSystem.sln --no-build --no-restore --verbosity minimal --logger 'trx;LogFileName=phase3.trx'
dotnet ef migrations has-pending-model-changes --project src/Hospital.Infrastructure --startup-project src/Hospital.Api --no-build
```

TRX nằm dưới `TestResults/` của mỗi project test, được gitignore vì là kết quả sinh tự động. `tests/Hospital.IntegrationTests/TestResults/phase3-api-smoke.json` ghi kết quả kiểm tra API chạy thật, không lưu token hay mật khẩu. Các file nguồn kiểm thử được lưu trong repository.

## Đối chiếu 18 tình huống bắt buộc

| # | Tình huống | Bằng chứng trong Hospital.IntegrationTests |
|---|---|---|
| 1 | Trùng username/email | ApiTests.AuthRejectsDuplicatesWrongPasswordInactiveAccountAndInvalidInput |
| 2 | Sai mật khẩu/tài khoản khóa | Cùng test auth; API trả 401 |
| 3 | Sai vai trò | ApiTests.WrongRoleIsForbidden, 5 trường hợp |
| 4 | Xem/sửa lịch của bệnh nhân khác | ApiTests.PatientOwnershipAndPaginationAreCheckedAtApi; AppointmentTests.PatientCannotBookForOrReadOrModifyAnotherPatient |
| 5 | Slot đã có người đặt | AppointmentTests.RejectsOccupiedSlotAndPatientOverlapButAllowsAdjacentSlots |
| 6 | Hai bệnh nhân đặt đồng thời | ConcurrencyTests.ConcurrentBookingsOnRelationalDatabaseHaveExactlyOneWinner và SqlServerConcurrencyTests.MigrationsAndConcurrentSlotBookingOnSqlServer đều passed |
| 7 | Bệnh nhân có hai lịch chồng lấn | Test occupied/overlap và ConcurrentOverlappingPatientBookingsOnSqlServer; có kiểm tra hai khoảng tiếp giáp vẫn hợp lệ |
| 8 | Đổi lịch còn dưới 2 giờ | PatientRescheduleEnforcesExactTwoHourBoundary, cả 1.99 giờ và đúng 2 giờ |
| 9 | Đổi lịch thất bại giữ lịch cũ | FailedRescheduleKeepsOldAppointmentSlotAndHistory; RescheduleSaveFailureRollsBackBothSlotsAndHistory |
| 10 | Hủy giải phóng slot, giữ lịch sử | CancellationReleasesSlotAndPreservesHistoryForRebooking; đặt lại slot bằng bệnh nhân khác thành công |
| 11 | Tiếp nhận lịch không hợp lệ | ReceptionOnlyAcceptsValidSameDayAppointmentAndIsIdempotent; CancelledAppointmentCannotBeReceivedOrMarkedNoShow |
| 12 | Ghi vắng khi chưa hết giờ | NoShowRequiresEndTimeStrictlyPassed |
| 13 | Đóng slot đang Confirmed | AdminAndScheduleTests.CannotCloseOrChangeScheduleWithConfirmedAppointments |
| 14 | Ngừng bác sĩ/chuyên khoa và giữ lịch sử | InactivationWithFutureAppointmentsIsBlockedThenKeepsHistoryAfterCancellation; ScheduleChangeRetainsCancelledSlotsAndTheirAppointmentHistory |
| 15 | Vô hiệu Admin cuối cùng | LastActiveAdminCannotBeDisabledOrDemoted và ConcurrentDisablingAdminsPreservesOneActiveAdminOnSqlServer đều passed |
| 16 | Gửi lại yêu cầu đặt | BookingIsAtomicAndIdempotentWithHistoryAuditAndNotification; API BookingThroughApiRequiresKeyAndVersionAndSupportsReplay; ConcurrentSameRequestReturnsOneAppointmentOnSqlServer |
| 17 | Database lỗi/lưu thất bại | ApiTests.DatabaseOutageReportsTraceAndLeavesNoPartialBooking; SaveFailureRollsBackSlotAppointmentHistoryAndOutbox; SqlServerBackendTests.UnavailableSqlDatabaseReturns503AndRecoversWithoutPartialBooking |
| 18 | Thông báo sau đặt/đổi/hủy | AppointmentTests và NotificationTests; có kiểm tra lỗi ghi thông báo, retry không trùng, đổi lịch hủy lời nhắc cũ và ngoại lệ đặt sát giờ |

Các test khác xác minh mã định danh, phân trang, lịch 30 phút, giới hạn vai trò bác sĩ, thay đổi TokenVersion, rate limit, giới hạn dữ liệu danh mục và stale RowVersion.

## Luồng HTTP đã xác minh

`ApiTests.FullBackendFlowAdminToPatientDoctorAndReceptionUsesOneDatabase` (SQLite) và `SqlServerBackendTests.FullBackendFlowUsesSqlServerThroughHttp` (SQL Server) cùng thực hiện qua HTTP:

1. Admin tạo hồ sơ bác sĩ có chuyên khoa rồi cấp tài khoản bác sĩ.
2. Admin tạo ca trực, backend sinh hai slot 30 phút.
3. Patient tra cứu slot và đặt lịch.
4. Doctor đăng nhập, xem lịch vừa tạo trong phạm vi của mình.
5. Staff tiếp nhận khi đến ngày khám.
6. Patient đọc lại trạng thái CheckedIn và thấy thông báo của lịch.

Mỗi lần chạy dùng một database thử nghiệm dùng chung cho các vai trò. Đây là kiểm thử backend qua HTTP, chưa phải demo giao diện Web/WinForms.

## Kết quả SQL Server

Lượt ngày 16/10/2026 còn thiếu SQL Server, với 58 passed và 4 skipped. Lượt ngày 17/10/2026 đã hoàn tất phần kiểm chứng trên SQLEXPRESS có sẵn, không cài thêm dịch vụ database.

Trước khi nâng cấp, đã tạo backup `COPY_ONLY, CHECKSUM` và chạy `RESTORE VERIFYONLY ... WITH CHECKSUM` thành công. Bản sao lưu trên máy:

```text
C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\HospitalDb_before_phase3_20261017_822968DD-118C-441A-93A1-27B9853F3096.bak
```

`HospitalDb` đã có cả `20261016125545_InitialCreate` và `20261016143621_Phase3Backend`. Sau nâng cấp vẫn có 6 tài khoản, 2 lịch hẹn; tên hai bác sĩ được chuyển từ tài khoản sang hồ sơ. Đã kiểm tra unique filtered index trên `(BookedBy, RequestKey)` và `TimeSlotId` cho trạng thái Confirmed/CheckedIn.

Bốn test passed trong `SqlServerConcurrencyTests`:

- Chạy migrations và hai bệnh nhân tranh cùng slot.
- Gửi cùng RequestKey đồng thời phải trả cùng một lịch.
- Một bệnh nhân đặt đồng thời hai slot chồng lấn thuộc hai bác sĩ.
- Hai thao tác khóa Admin đồng thời phải giữ lại ít nhất một Admin hoạt động.

Ba test passed trong `SqlServerBackendTests`:

- `FullBackendFlowUsesSqlServerThroughHttp`: luồng Admin tạo bác sĩ/tài khoản/ca trực, Patient đặt, Doctor xem, Staff tiếp nhận, Patient đọc trạng thái và thông báo.
- `RealRowVersionRejectsStaleApiAndDatabaseWrites`: SQL Server sinh rowversion 8 byte và thay đổi sau reschedule; API từ chối phiên bản cũ bằng 409; EF từ chối ghi đè từ DbContext đã đọc phiên bản cũ, giữ lịch mới và lịch sử.
- `UnavailableSqlDatabaseReturns503AndRecoversWithoutPartialBooking`: kết nối tới catalog thử nghiệm không tồn tại tạo lỗi SQL thật 4060. Readiness và yêu cầu đặt trả 503; lỗi có trace, không lộ tên database; không tạo lịch hoặc chiếm slot. Sau phục hồi kết nối, gửi lại RequestKey tạo đúng một lịch.

Test cuối đã phát hiện middleware trả 500 khi SQL error bị bọc trong `RetryLimitExceededException`. Đã sửa phân loại lỗi bên trong wrapper và xác nhận test qua. Thử lỗi kết nối không tắt SQL Server dùng chung, không gây gián đoạn `HospitalDb`; đây không phải thử mất điện hoặc ngắt mạng giữa lúc commit.

Theo hướng dẫn trong `api-documentation.md`, đặt `HOSPITAL_TEST_SQLSERVER` rồi chạy nhóm `FullyQualifiedName~SqlServer`. Fixture tạo database `HospitalPhase3Tests_<GUID>`, kiểm tra tiền tố trước khi xóa; không dùng hoặc xóa `HospitalDb`. Không cấu hình biến này sẽ Skip 7 test, không đủ điều kiện nghiệm thu SQL Server.

## API chạy với HospitalDb

Đã chạy `dotnet run --project src/Hospital.Api --no-build --no-restore --launch-profile http` và kiểm tra:

| Request | Kết quả |
|---|---|
| `GET /`, `GET /health/ready` | 200 |
| `GET /swagger/index.html`, `GET /swagger/v1/swagger.json` | 200 |
| Đăng nhập admin, staff1, doctor1, patient1 | 200 cho cả 4 tài khoản |
| Admin đọc `/api/admin/users` | 200 |
| Staff đọc `/api/appointments` | 200 |
| Doctor đọc `/api/appointments/doctor` | 200 |
| Patient đọc `/api/appointments/my` | 200 |

API được dừng sau khi kiểm tra; có thể chạy lại bằng lệnh trên. Phase 3 đã hoàn tất trong phạm vi backend. Phase 4–7 vẫn cần thiết kế giao diện, triển khai Web/Desktop và kiểm thử tích hợp sản phẩm thực tế.
