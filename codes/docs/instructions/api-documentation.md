# Backend Phase 3

Backend phục vụ chung cho Web MVC và WinForms. Các ứng dụng giao diện được triển khai ở Phase 5–6. Tài liệu này mô tả API hiện có và cách kiểm tra backend độc lập.

## Chạy trên máy phát triển

Target hiện tại là .NET 9, EF Core 9.0.2. Ngày 17/10/2026 đã build và test bằng SDK 10.0.400 cùng runtime .NET/ASP.NET Core 9.0.1 trên SQL Server 2022 Express. Không cần roll-forward trên máy này. Nếu chỉ có runtime .NET 10, có thể đặt `DOTNET_ROLL_FORWARD=Major` như lần kiểm thử trước; giữ nguyên target của các project.

Tại thư mục `codes` chứa `HospitalAppointmentSystem.sln` (nếu đang ở gốc repository, chạy `cd codes`):

```powershell
dotnet restore HospitalAppointmentSystem.sln
dotnet tool restore
dotnet build HospitalAppointmentSystem.sln --no-restore
```

Cấu hình SQL Server tại biến môi trường; ví dụ dùng Windows Authentication:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=.\SQLEXPRESS;Database=HospitalDb;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet ef database update --project src/Hospital.Infrastructure --startup-project src/Hospital.Api
$env:Database__SeedDemo = 'true'
dotnet run --project src/Hospital.Api --launch-profile http
```

Development tự áp dụng migrations trước khi nhận request. `Database__SeedDemo=true` cho phép tạo dữ liệu demo khi chưa có tài khoản. Mật khẩu demo chỉ phục vụ development và có trong tài liệu database. Không bật seed demo cho môi trường thật. Seeder không sửa hoặc đặt lại mật khẩu tài khoản đã có.

Swagger ở `http://localhost:5014/swagger`. `GET /` là kiểm tra tiến trình; `GET /health/ready` kiểm tra kết nối database và trả 503 nếu chưa sẵn sàng. Thăm dò readiness không cần token. Lỗi khởi tạo/migration khiến startup thất bại, tránh báo hệ thống sẵn sàng khi database chưa được nâng cấp.

Trong development, nếu chưa cấu hình khóa JWT, API tạo khóa ngẫu nhiên mỗi lần chạy; khởi động lại làm hết hiệu lực token cũ. Môi trường khác bắt buộc cấu hình `JwtSettings__Secret` ít nhất 32 byte, cùng issuer/audience phù hợp; sử dụng secret manager hoặc biến môi trường. Cấu hình cục bộ khác có thể để trong `appsettings.Local.json` đã được gitignore. CORS mặc định cho hai URL Web localhost 5174/7122, có thể thay qua `Cors:AllowedOrigins`.

## Quy ước dữ liệu

- Gửi `Authorization: Bearer <token>` cho API nghiệp vụ.
- Tài khoản bị khóa, đổi vai trò hoặc đổi mật khẩu làm mất hiệu lực các token cũ. API kiểm tra lại tài khoản và phiên tại mỗi request.
- Ngày/giờ khám là giờ Việt Nam (`Asia/Ho_Chi_Minh`, UTC+7); dấu thời gian sự kiện và lịch gửi thông báo lưu UTC.
- Enum trong response là chuỗi: `Confirmed`, `Cancelled`, `CheckedIn`, `NoShow`. Client C# dùng `JsonStringEnumConverter` khi đọc DTO.
- Danh sách có phân trang trả `data.items`, `totalCount`, `page`, `pageSize`; trang bắt đầu từ 1, pageSize tối đa 100. Lịch hẹn mặc định tăng theo ngày/giờ, có `descending=true`.
- Lịch làm việc trả danh sách theo khoảng ngày, mặc định 31 ngày, tối đa 366 ngày mỗi lần. Khung giờ trống được giới hạn theo một bác sĩ/một ngày.
- Lỗi có `success=false`, `message`, `errorCode`, `traceId`, `timestamp`; lỗi validation có thêm `errors`. Không trả stack trace hoặc thông tin kết nối cho client.
- HTTP: 400 dữ liệu/quy tắc không hợp lệ; 401 chưa đăng nhập/phiên hết hiệu lực; 403 sai vai trò; 404 không tồn tại hoặc ngoài phạm vi sở hữu; 409 trùng dữ liệu/xung đột phiên bản; 429 quá giới hạn đăng nhập/đăng ký; 503 SQL Server chưa đáp ứng; 500 lỗi khác.
- Khi EF đã thử kết nối lại hết số lần cho phép, middleware đọc lỗi SQL bên trong `RetryLimitExceededException` và vẫn trả 503 `DATABASE_UNAVAILABLE`; không trả chi tiết kết nối cho client.

## Các endpoint

| Nhóm | Endpoint | Quyền |
|---|---|---|
| UC01–02 | `POST /api/auth/register`, `POST /api/auth/login` | Công khai, giới hạn 10 request/phút/IP |
| UC03–04 | `GET/PUT /api/auth/profile`, `POST /api/auth/change-password` | Người dùng đang đăng nhập, hồ sơ của mình |
| UC05 | `GET /api/specialties`, `GET /api/specialties/{id}` | Patient, Staff, Admin |
| UC06–07 | `GET /api/doctors`, `GET /api/doctors/{id}` | Patient, Staff, Admin |
| UC08 | `GET /api/timeslots/available?doctorId=...&date=yyyy-MM-dd` | Patient, Staff, Admin |
| UC09 | `GET /api/schedules`, `GET /api/schedules/{id}`, `GET /api/schedules/my` | Admin/Doctor; `/my` chỉ Doctor; Doctor chỉ xem mình |
| UC10–11 | `POST /api/schedules`, `PUT /api/schedules/{id}` | Admin |
| UC12 | `POST /api/timeslots/{id}/close` | Admin; body `{ "reason": "..." }` |
| UC13–14 | `POST /api/appointments` | Patient, Staff; UC14 là kiểm tra dùng chung trong service |
| UC15 | `GET /api/appointments/my`, `/doctor`, `/{id}` | Patient xem mình; Doctor xem lịch thuộc mình; Staff xem phạm vi tiếp nhận |
| UC15–16 | `GET /api/appointments`, `/search` | Staff |
| UC17–18 | `POST /api/appointments/{id}/reschedule`, `/cancel` | Patient, Staff |
| UC19–20 | `POST /api/appointments/{id}/check-in`, `/no-show` | Staff |
| UC21 | `GET /api/notifications`, `PUT /api/notifications/{id}/read` | Patient, thông báo của mình |
| Hỗ trợ UC13 | `GET /api/patients?keyword=...`, `GET /api/patients/{id}` | Staff tra cứu đúng hồ sơ đã có; không tự cấp tài khoản bệnh nhân |
| UC22–24 | `GET/POST /api/admin/users`, `GET/PUT /api/admin/users/{id}`, `PUT /api/admin/users/{id}/role` | Admin |
| UC25–26 | `POST /api/admin/doctors`, `PUT /api/admin/doctors/{id}` | Admin |
| UC27–28 | `POST /api/admin/specialties`, `PUT /api/admin/specialties/{id}` | Admin |
| Nhật ký | `GET /api/admin/audit-logs?entityType=...` | Admin |

Tra cứu danh mục hỗ trợ `keyword`, `page`, `pageSize`; bác sĩ có thêm `specialtyId`. `onlyActive=false` chỉ dành cho Admin. Danh sách lịch hẹn hỗ trợ `keyword` (mã hẹn/tên/số điện thoại), `fromDate`, `toDate`, `status`, `doctorId`, `patientId`, `page`, `pageSize`, `descending`. Các bộ lọc không mở rộng phạm vi sở hữu của Patient/Doctor.

## Ví dụ đặt và thay đổi lịch

```json
{
  "timeSlotId": "GUID_KHUNG_GIO",
  "specialtyId": 1,
  "requestKey": "GUID_MOI_CHO_MOT_YEU_CAU_DAT",
  "notes": "Lý do khám"
}
```

Patient được xác định từ phiên; không thể gửi PatientId của người khác. Staff phải cung cấp `patientId` hợp lệ. Giữ nguyên `requestKey` khi gửi lại cùng yêu cầu sau lỗi mạng. Cùng khóa/nội dung trả lại lịch hiện có; cùng khóa/nội dung khác trả 409. Khóa được xác định trong phạm vi người đặt. Tạo lịch mới thành công trả 201; gửi lại cũng trả 201 với cùng Id.

Response lịch hẹn chứa `rowVersion` dạng base64. Gửi nguyên giá trị đó khi đổi/hủy/tiếp nhận/ghi vắng:

```json
{
  "rowVersion": "GIA_TRI_TU_RESPONSE",
  "reason": "Bệnh nhân yêu cầu thay đổi"
}
```

Đổi lịch thêm `timeSlotId`, `specialtyId` mới. Patient chỉ đổi/hủy Confirmed khi còn ít nhất 2 giờ; Staff trước giờ bắt đầu và bắt buộc lý do. Phiên bản cũ trả 409, cần đọc lại lịch. Gửi lại hủy/tiếp nhận/ghi vắng đã thành công trả trạng thái hiện có và không tạo thêm lịch sử. Đổi lịch gửi lại với phiên bản cũ có thể trả 409; tra cứu kết quả trước khi xác nhận lần nữa.

Tiếp nhận yêu cầu nhân viên đã đối chiếu đúng bệnh nhân, đúng ngày khám và trước khi khung giờ kết thúc; đặc tả không quy định cửa sổ đến sớm theo số phút nên không bổ sung một mốc tùy ý. Chỉ ghi NoShow khi `EndTime < Now`. CheckedIn vẫn chiếm slot và không có nghĩa đã hoàn tất khám.

## Quyết định kỹ thuật

- Đặt/đổi/hủy, thay lịch làm việc và thao tác quản trị dùng transaction `Serializable` trong execution strategy của EF. Retry đọc lại dữ liệu và bỏ tracking của lần thất bại. Tham khảo [EF transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions) và [connection resiliency](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency).
- Một slot có nhiều lịch hẹn lịch sử; unique filtered index chỉ cho một bản ghi Confirmed/CheckedIn. RowVersion bảo vệ sửa đồng thời; kiểm tra chồng lấn bệnh nhân chạy trong cùng transaction với thao tác lưu.
- Khi điều chỉnh ca trực, slot cũ được đóng và giữ lại; không xóa liên kết lịch đã hủy. Chặn ca còn Confirmed/CheckedIn. Hồ sơ bác sĩ giữ họ tên độc lập trước khi cấp tài khoản.
- Mỗi bác sĩ có một chuyên khoa hiện hành qua `SpecialtyId`. Lịch hẹn giữ chuyên khoa đã ghi để bảo toàn lịch sử. Thay vai trò phải phù hợp hồ sơ; tài khoản bệnh nhân giữ vai trò Patient để bảo toàn quyền sở hữu. Không khóa/thu hồi quyền Admin hoạt động cuối cùng.
- Sự kiện thông báo và lịch hẹn được ghi vào outbox trong cùng transaction. Dispatcher ghi Notification sau commit, có khóa duy nhất OutboxId. Lỗi gửi không đảo ngược lịch đã lưu; worker thử lại mỗi 30 giây. Đổi/hủy/tiếp nhận/vắng hủy reminder chưa gửi. Reminder kiểm tra lại lịch trước khi phát; lịch đặt trong vòng 1 giờ chỉ có xác nhận.
- `NotificationOutbox` là bảng hỗ trợ xử lý thông báo. Migration `Phase3Backend` giữ dữ liệu Phase 2, lấy tên bác sĩ từ tài khoản, chuyển thông báo sự kiện cũ sang đã hiển thị và tạo reminder cho lịch cũ còn hợp lệ.

## Nâng cấp và kiểm thử

`phase3-upgrade.sql` là script idempotent nâng từ migration InitialCreate lên Phase3Backend, dành cho database Phase 2. Database mới dùng `dotnet ef database update` để chạy đầy đủ migrations. Không downgrade về quan hệ 1–1 nếu một slot đã có nhiều lịch sử; migration sẽ từ chối thay vì xóa lịch sử.

```powershell
dotnet test HospitalAppointmentSystem.sln --no-restore --logger trx
```

Kết quả và giới hạn kiểm chứng được ghi trong `phase3-verification.md`. Bộ test SQL Server sử dụng database mới có tên `HospitalPhase3Tests_<GUID>` và chỉ xóa database đó khi hoàn tất. Connection phải có quyền tạo/xóa database thử nghiệm:

```powershell
$env:HOSPITAL_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet test tests/Hospital.IntegrationTests --filter FullyQualifiedName~SqlServer
```

Nhóm SQL Server gồm 4 test cạnh tranh và 3 test backend (luồng HTTP, rowversion, lỗi kết nối/phục hồi). Không có biến này, 7 test được báo Skip rõ ràng. Khi nghiệm thu Phase 3 phải cấu hình biến này và bảo đảm không có test SQL Server bị Skip. Kết quả ngày 17/10/2026: 65 passed, 0 failed, 0 skipped cho toàn solution. SQLite chỉ dùng trong một phần bộ test; ứng dụng chính dùng SQL Server.


## Bổ sung Phase 5 — 18/10/2026

`GET /api/patients/{id}` phục vụ Staff đối chiếu hồ sơ trước bước xác nhận đặt hộ. Trả `ApiResponse<UserProfileDto>` với PatientId/mã/họ tên/ngày sinh/giới tính/điện thoại/địa chỉ; chỉ lấy bệnh nhân và tài khoản Patient đang hoạt động. Id không hợp lệ hoặc hồ sơ inactive trả 404; Patient/Doctor/Admin không được gọi. Không có endpoint tạo tài khoản bệnh nhân cho Staff.

Web dùng hợp đồng đặt/đổi/hủy/tiếp nhận hiện có; bản xác nhận có thời hạn, mã hóa và gắn người dùng, giữ RequestKey/RowVersion khi thử lại. Chi tiết và bằng chứng nằm trong `phase5-verification.md`. Tổng nghiệm thu mới: 79 .NET tests, gồm 8 SQL Server tests (thêm luồng Web đặt lịch/mất phản hồi), cùng 11 browser tests. Mốc 65 tests/7 SQL ở phần trên là kết quả riêng Phase 3.


## Bổ sung Phase 6 — 18/10/2026

- `GET /api/admin/users/{id}` chỉ cho Admin, trả `UserDto` hiện tại với thông tin liên hệ/vai trò/trạng thái/DoctorId; không trả mật khẩu. Id không tồn tại trả 404. Desktop đọc lại trước khi mở form sửa/phân quyền.
- `DoctorDto.linkedUserId` chỉ có giá trị trong response dành cho Admin; Patient/Staff nhận null. Doctor vẫn không có quyền đọc danh mục. Sau OOAD A2, `DoctorDto` trả `specialtyId`/`specialtyName`; `DoctorDetailDto` trả `specialtyId`/`specialty`. Ghi hồ sơ bằng `SaveDoctorRequest.specialtyId`; các trường danh sách và chuyên khoa chính cũ đã bỏ. Xem [migration và hợp đồng A2](single-specialty-migration.md).
- Nhật ký trả `PagedResult<AuditLogDto>` gồm `id`, `userId`, `userName`, `action`, `entityType`, `entityId`, `oldValues`, `newValues`, `createdAt`. Tên người thực hiện lấy từ tài khoản hiện tại; thời gian lưu UTC, Desktop hiển thị UTC+7.
- Desktop dùng nguyên các API ghi đã có, không xóa cứng hoặc kết nối SQL trực tiếp. Khi mất phản hồi ghi, đóng form và đọc lại kết quả trước khi gửi yêu cầu mới.

Không có migration mới. Nghiệm thu: 82 .NET tests (8 SQL Server) và 87 assertions WinForms trên API/SQL Server riêng; xem `phase6-verification.md`.
