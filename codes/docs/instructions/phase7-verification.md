# Kiểm chứng Phase 7 — 18/10/2026

Phạm vi: tích hợp Windows Forms Admin ↔ Web Patient/Doctor/Staff ↔ HTTP API ↔ SQL Server, 18 kịch bản bắt buộc, cạnh tranh, transaction, thông báo và hồi quy. Không sửa API nghiệp vụ, không thêm migration hoặc endpoint phục vụ test vào ứng dụng. Phase 8 vẫn dành cho rà soát cuối, demo và bàn giao.

## Kết quả

| Kiểm tra | Kết quả |
|---|---|
| Build solution và project kiểm thử bổ sung | 0 warning, 0 error |
| .NET toàn solution | 102 passed, 0 failed, 0 skipped: 8 unit + 88 integration + 6 Web |
| SQL Server trong suite | 28 tests: 8 trước Phase 7 + 18 kịch bản bắt buộc + lỗi ghi/outbox + timeout SQL thật |
| Chuỗi Chromium–WinForms mới | 1 passed; tạo danh mục/ca qua Desktop, đặt/tiếp nhận/đổi/hủy/thông báo qua Web và đóng khung qua Desktop |
| Hồi quy Chromium | 11 passed: 8 Phase 4 + 3 Phase 5; tổng cộng 12 browser tests |
| Hồi quy WinForms | 89 assertions đạt; message loop Windows thật, cửa sổ ẩn |
| Đối chiếu SQL sau chuỗi giao diện | Đúng 3 lịch của bác sĩ mới; đúng lịch sử, trạng thái tiếp nhận/hủy/đóng, 3 thông báo cho luồng đặt–đổi–hủy; không có slot chứa hai lịch có hiệu lực |
| Design tokens | Đồng bộ |

Môi trường: Windows, SDK .NET 10.0.400, runtime 9.0.1, SQL Server Express `.\SQLEXPRESS`, Chromium qua Playwright đã cài tại máy. Tất cả kiểm thử ghi dữ liệu dùng database riêng; không dùng HospitalDb.

## Chuỗi xuyên giao diện

`phase7.spec.js` gọi DesktopSmoke bằng process Windows ẩn tại bốn bước `setup`, `confirmed`, `received`, `released`. Harness mở **các form sản phẩm thật**, điền trường và bấm Lưu; modal chọn hồ sơ bác sĩ chạy thật, hộp xác nhận được tự trả lời qua `IDesktopDialogs`. API và SQL Server không bị mock. File fixture chỉ chứa Id/tên/ngày thử nghiệm, không chứa JWT/cookie.

1. WinForms tạo chuyên khoa, bác sĩ, tài khoản Doctor, ca hôm nay và ca sau ba ngày. Patient Web tìm thấy bác sĩ và chọn được slot vừa tạo.
2. Patient đặt lịch hôm nay; Doctor xem lịch Đã xác nhận. Bệnh nhân khác truy cập chi tiết nhận 404. Desktop thấy slot Occupied, nút đóng bị khóa; gửi yêu cầu từ form đóng cũng bị API từ chối.
3. Staff đối chiếu và tiếp nhận. Patient và Doctor cùng thấy **Đã tiếp nhận**. Desktop tiếp tục chặn đóng slot CheckedIn. Không diễn giải CheckedIn là hoàn tất khám.
4. Patient đặt lịch tương lai, gửi lại đúng POST xác nhận và nhận cùng Id; đổi giờ rồi hủy. Web hiển thị đúng ba thông báo; đánh dấu một thông báo đã đọc.
5. Hai browser context của hai Patient cùng xem lại một slot còn trống. Test giữ hai POST `/Booking/Confirm` ở ranh giới trình duyệt, chỉ giải phóng khi cả hai đã gửi. Đúng một response chuyển tới phiếu hẹn, response còn lại hiển thị lỗi slot không khả dụng. Không mock response hoặc thay dữ liệu trong test cạnh tranh.
6. Staff lọc đúng bác sĩ/ngày/trạng thái và thấy một lịch có hiệu lực từ cuộc đua. Patient thắng hủy lịch; Desktop thấy các slot tương lai được giải phóng và đóng một slot bằng form có lý do.
7. Patient tải lại danh sách: slot vừa đóng biến mất, slot đã hủy còn lại có thể chọn. Truy vấn SQL xác nhận không có lịch trùng do cạnh tranh hoặc gửi lặp, đủ lịch sử/thông báo và trạng thái đọc.

Đã xem ảnh Patient sau tiếp nhận và trang báo lỗi cho người thua cuộc đua. Hồi quy browser tiếp tục kiểm tra responsive 320–1440px và axe trên các màn hình đã chọn từ Phase 4–5.

## 18 kịch bản bắt buộc

Mỗi dòng tương ứng một case `Phase7AcceptanceTests.RequiredScenario(number, description)`, chạy qua host HTTP API thật trên một SQL Server database mới được migrate/seed riêng. Các assertion đọc lại database để xác nhận kết quả hoặc dữ liệu không bị sửa sau lỗi.

| # | Kịch bản | Bằng chứng chính |
|---|---|---|
| 1 | Trùng username/email | Cả hai trả 409, số tài khoản không tăng |
| 2 | Sai mật khẩu/tài khoản khóa | 401; token đã cấp trước khi khóa cũng bị thu hồi |
| 3 | Trái vai trò | Patient/Staff không vào Admin, Admin không đặt lịch |
| 4 | Xem/sửa lịch người khác | GET, hủy và đổi đều 404; slot/lịch sử giữ nguyên |
| 5 | Đặt slot đã kín | Yêu cầu thứ hai bị từ chối; chỉ một lịch lưu trên slot |
| 6 | Hai Patient đặt đồng thời | Hai HTTP request cùng được giải phóng; đúng một 201, SQL có một Confirmed; thêm cuộc đua thật từ hai browser context |
| 7 | Bệnh nhân có lịch chồng lấn | Bị từ chối dù hai bác sĩ khác nhau; slot thứ hai vẫn Open |
| 8 | Đổi khi còn dưới 2 giờ | Đồng hồ test đặt tại mốc 119 phút; lịch cũ giữ nguyên |
| 9 | Đổi vào slot đã bị chiếm | Id slot, RowVersion, lịch sử giữ nguyên; hai slot vẫn thuộc đúng hai lịch |
| 10 | Hủy giải phóng slot | Cancelled, thêm lịch sử, slot Open và Patient khác đặt lại thành công |
| 11 | Tiếp nhận không hợp lệ | Chặn lịch Cancelled và lịch quá giờ; lịch quá giờ vẫn Confirmed |
| 12 | NoShow quá sớm | Đúng thời điểm kết thúc vẫn bị chặn; một giây sau thành công và giải phóng slot |
| 13 | Đóng slot có lịch | Chặn cả Confirmed và CheckedIn; slot vẫn Occupied |
| 14 | Ngừng hoạt động có lịch sử | Bác sĩ/chuyên khoa inactive; lịch đã hủy và lịch sử vẫn đọc được |
| 15 | Admin cuối | Chặn khóa và hạ vai trò; SQL vẫn có một Admin hoạt động |
| 16 | Gửi lại đặt thành công | Cùng Id, chỉ một lịch/một lịch sử đặt/một audit đặt; browser cũng gửi lại đúng POST |
| 17 | Database không truy cập được | 503 + trace, không lộ connection string, không có ghi dở; phục hồi và gửi lại thành công |
| 18 | Thông báo sau đặt/đổi/hủy | Đúng ba loại, ban đầu chưa đọc; dispatcher chạy lặp không nhân đôi, Patient khác không đánh dấu hộ |

Case 17 tái sử dụng `SqlServerBackendTests.UnavailableSqlDatabaseReturns503AndRecoversWithoutPartialBooking`. Kết nối lỗi chỉ trỏ tới tên database không tồn tại trong fixture, không dừng SQL Server dùng chung. Đồng hồ điều khiển được chỉ nằm trong test host, không thêm tùy chọn đổi giờ cho app đang triển khai.

`SqlServerWriteFailureRollsBackMutationAndOutboxDeliveryCanRecover` bổ sung lỗi tại SaveChanges: đổi lịch thất bại không đổi slot/RowVersion/lịch sử/outbox; lỗi ghi Notification không đảo ngược lịch đã commit, dispatcher phục hồi đúng một lần.

`SqlServerTimeoutTests.CommandTimeoutAfterWriteRollsBackBothSlotsHistoryAuditAndOutbox` tạo timeout SQL thực sự: interceptor thêm một UPDATE thăm dò vào **transaction của ứng dụng**, tiếp theo `WAITFOR DELAY` ba giây với CommandTimeout một giây. Xác nhận nhận SqlException `Number=-2`, UPDATE thăm dò bị rollback, hai slot/lịch hẹn/RowVersion/lịch sử/audit/outbox không có thay đổi dở dang. Chỉ context thử lỗi này tắt retry để quan sát timeout đầu tiên; cấu hình retry sản phẩm giữ nguyên.

## Chạy lại toàn bộ

Từ `codes`, đã cài SDK/runtime .NET 9 phù hợp, SQL Server Windows Authentication và `sqlcmd`, Node, Python:

```powershell
dotnet restore HospitalAppointmentSystem.sln -m:1
Push-Location tests/browser
npm ci
npx playwright install chromium
Pop-Location
powershell -NoProfile -File scripts/verify-phase7.ps1
```

Có thể đặt `PLAYWRIGHT_CHROMIUM_PATH` tới Chromium đã cài. Nếu chính sách PowerShell của máy không cho chạy script, dùng cách cho phép script đã kiểm tra theo chính sách máy. Không cần tắt auth rate limiter.

Runner tự build, đặt `HOSPITAL_TEST_SQLSERVER`, chạy toàn bộ .NET tests, tạo `HospitalPhase7Ui_<GUID>`, khởi động API/Web bằng cửa sổ ẩn, chạy chuỗi Phase 7, đối chiếu SQL, chạy 11 browser tests cũ và DesktopSmoke cũ. Có hai khoảng chờ 60 giây giữa nhóm đăng nhập để tuân thủ rate limiter. Mỗi bước Desktop trong chuỗi browser có giới hạn 60 giây; hồi quy Desktop có giới hạn 120 giây để báo lỗi và dọn process nếu modal bị treo. Dừng service và xóa **đúng database do runner tạo** trong `finally`, kể cả khi test thất bại. Từ chối dùng cổng đã có service chạy; không dừng process của người dùng.

```powershell
# Chọn instance/cổng khác nếu cần:
powershell -NoProfile -File scripts/verify-phase7.ps1 -SqlServer '.\SQLEXPRESS' -ApiPort 5036 -WebPort 5177
# Chạy riêng nhóm SQL mới sau khi build:
$env:HOSPITAL_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet test tests/Hospital.IntegrationTests --no-build --no-restore --filter 'FullyQualifiedName~Phase7AcceptanceTests|FullyQualifiedName~SqlServerTimeoutTests'
```

Tham số `-SkipBuild`, `-SkipDotNet`, `-SkipRegression` chỉ phục vụ điều tra từng phần; không dùng kết quả chạy thiếu nhóm để nghiệm thu đầy đủ. Không có biến SQL, các nhóm SQL báo skip; nghiệm thu phải cấu hình biến và đạt **0 skipped**. Không chạy hai runner trên cùng cổng. Chuỗi browser cần chạy trước **22:30 giờ Việt Nam** để tạo ca còn trong ngày và kiểm chứng tiếp nhận thật; ngoài khoảng này harness báo lỗi rõ ràng, không bỏ qua bước. Nhóm SQL kiểm tra thời gian với đồng hồ cố định nên chạy được mọi lúc.

## Bằng chứng và giới hạn

Lượt chuỗi giao diện/hồi quy cuối lưu tại `tests/browser/artifacts/phase7/c5a8ec2167554112b0010ce44af0ad0e/`: `fixture.json`, `result.json`, `desktop.log`, ảnh, log build/test/API/Web, `sql-evidence.log`, `cleanup.log`. Kiểm thử .NET trong cùng lượt chạy có TRX `phase7.trx` dưới từng project TestResults. Các artifact chứa dữ liệu demo và có thể chứa cookie trong trace nên giữ gitignore; không commit build output hoặc artifact.

Lượt thử đầu chuỗi UI đã đạt nhưng câu truy vấn SQL đối chiếu nhầm giá trị Closed của TimeSlot với giá trị enum khác; đã sửa query theo `TimeSlotStatus.Closed = 1`. Log Desktop được xuất UTF-8 để đọc đúng tiếng Việt khi gọi từ Node.

Runner giữ native process handle để Windows PowerShell đọc đúng `ExitCode` sau khi Desktop kết thúc; đã kiểm chứng riêng cả mã 0 và mã 7, tránh báo lỗi khi smoke thành công mà vẫn phát hiện process thất bại.

Hồi quy trên dữ liệu đã có bác sĩ Phase 7 phát hiện nút chọn hồ sơ giữ trạng thái của dòng trước: `SelectionChanged` xảy ra trước khi `CurrentRow` cập nhật, khiến chuyển từ bác sĩ đã liên kết sang hồ sơ còn trống vẫn bị khóa nút. Đã dùng `CurrentCellChanged` cho DoctorPicker, các thao tác theo dòng ở AdminShell và ScheduleDetails; smoke kiểm tra chuyển từ dòng đã liên kết sang dòng chưa liên kết. Đây là sửa lỗi trạng thái giao diện; các quy tắc API và transaction giữ nguyên.

Đây là kiểm chứng Chromium và WinForms chạy ẩn bằng message loop thật, không phải nghiệm thu thao tác GUI thủ công toàn diện, screen reader, nhiều DPI hay mọi trình duyệt. Cạnh tranh được xác nhận trên SQL Server và hai phiên browser; không phải thử tải quy mô lớn. Phase 8 tiếp tục hoàn thiện trải nghiệm và tài liệu demo/bàn giao.
