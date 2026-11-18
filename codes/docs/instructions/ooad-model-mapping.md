# Ánh xạ mô hình báo cáo lần 04 sang mã nguồn

Tài liệu này hoàn thành A1 của kế hoạch OOAD: xác định cách 14 lớp trong Hình 5.1–5.4 và Hình 6.1 tương ứng với triển khai, chỉ rõ phần còn thiếu. Cập nhật cùng đợt A2 chuyển bác sĩ về một chuyên khoa. Đường dẫn mã nguồn bên dưới tính từ `codes/src`.

## Các lớp dữ liệu

| Lớp báo cáo | Entity và bảng hiện tại | Service và DTO liên quan | Màn hình và use case | Mức đáp ứng và việc tiếp theo |
|---|---|---|---|---|
| VaiTro | `Hospital.Domain/Entities/Role.cs` → `Roles` | `AdminService`, `ChangeRoleRequest`, `UserDto` | Admin phân quyền, UC24 | Có 4 vai trò; mã triển khai là enum/int |
| TaiKhoan | `User.cs` → `Users` | `AuthService`, `AdminService`; `AuthResponseDto`, `UserProfileDto`, DTO tài khoản Admin | Web Account và Desktop tài khoản, UC01–04/22–24 | Có xác thực, hồ sơ, trạng thái và phiên; phương thức cập nhật dữ liệu hiện chủ yếu nằm trong service |
| BenhNhan | `Patient.cs` → `Patients` | `AuthService`, `AdminService.PatientsAsync`; `UserProfileDto` | Đăng ký/hồ sơ, Staff tìm bệnh nhân, UC01/03/13 | Liên kết tài khoản duy nhất; họ tên hiện lấy từ User, cần nêu rõ khi giải thích mô hình |
| ChuyenKhoa | `Specialty.cs` → `Specialties` | `CatalogService`, `AdminService.SaveSpecialtyAsync`; `SpecialtyDto`, `SaveSpecialtyRequest` | Catalog chuyên khoa, Admin danh mục, UC05/27–28 | Có; A2 dùng quan hệ một chuyên khoa có nhiều bác sĩ |
| BacSi | `Doctor.cs` → `Doctors` | `CatalogService`, `AdminService.SaveDoctorAsync`; `DoctorDto`, `DoctorDetailDto`, `SaveDoctorRequest` | Chi tiết bác sĩ, form Admin, UC06–07/25–26 | A2: `SpecialtyId` bắt buộc, một chuyên khoa hiện hành; tài khoản liên kết tùy chọn |
| QuyTacLich | Chưa có | Dự kiến `ScheduleRule` và service lập lịch định kỳ | Admin lịch làm việc, UC09–11 | A3/B2/B4/B5 bổ sung khoảng áp dụng, trạng thái và phiên bản quy tắc |
| MauLapTuan | Chưa có | Dự kiến `WeeklyPattern`, DTO thứ/ca | Admin tạo/sửa lịch định kỳ, UC10–11 | A3 bổ sung khóa ghép quy tắc/thứ/ca |
| CaTheoNgay | `WorkingSchedule.cs` → `WorkingSchedules` | `ScheduleService`; `WorkingScheduleDto`, DTO tạo/sửa | Doctor lịch, Admin lịch làm việc, UC09–12 | Có ngày/giờ/bác sĩ; thiếu nguồn quy tắc, mã ca, trạng thái ca và dấu chỉnh riêng |
| KhungGio | `TimeSlot.cs` → `TimeSlots` | `ScheduleService`, `AppointmentService`; `TimeSlotDto`, `CloseTimeSlotRequestDto` | Chọn giờ và chi tiết ca, UC08/12–14/17 | Có; hiện 30 phút, chuyển 20 phút ở B1; bổ sung mở lại và ràng buộc ở A4/B6 |
| LichHen | `Appointment.cs` → `Appointments` | `AppointmentService`; `AppointmentDto`, DTO đặt/đổi/hủy/tiếp nhận | Web Booking/Appointments/Reception, UC13–20 | Có 4 trạng thái, transaction và RowVersion; lưu chuyên khoa tại thời điểm đặt để giữ lịch sử |
| SuKienLichHen | `AppointmentStatusHistory.cs` → `AppointmentStatusHistories` | `AppointmentService.History`; `AppointmentHistoryDto` | Chi tiết lịch hẹn | Có trạng thái, người, thời điểm và ID khung trước/sau; A5 bổ sung loại sự kiện và FK khung giờ |
| LichNhac | Chưa có thực thể độc lập; hiện nhắc dùng `NotificationOutbox` | `AppointmentService.Reminder`, `SuppressRemindersAsync`, `NotificationDispatcher` | Xử lý nội bộ và Web thông báo, BR10 | A6 bổ sung liên kết sự kiện và trạng thái lịch nhắc; outbox là cơ chế xử lý, không đồng nhất với lớp báo cáo |
| ThongBao | `Notification.cs` → `Notifications` | `NotificationService`, `NotificationDispatcher`; `NotificationDto` | Web Notifications, UC21 | Có thông báo trong ứng dụng; A6 chuyển truy vết và chống trùng theo sự kiện/loại |
| NhatKyThaoTac | `AuditLog.cs` → `AuditLogs` | `DatabaseWork.Audit`, `AdminController`; `AuditLogDto` | Desktop nhật ký, thao tác cập nhật/quản trị | Có tác nhân/thời điểm và dữ liệu trước/sau; A7 bổ sung mã yêu cầu/kết quả cho chống gửi lặp |

`DoctorSpecialty` và bảng `DoctorSpecialties` thuộc thiết kế cũ nhiều–nhiều. A2 loại bỏ chúng khỏi mô hình hiện hành; migration cũ được giữ để bảo toàn chuỗi nâng cấp. `NotificationOutbox`, RowVersion và TokenVersion là chi tiết triển khai bổ sung, không phải các lớp nghiệp vụ thay thế cho phần còn thiếu.

## Từ sơ đồ tuần tự đến triển khai

| Trách nhiệm mô hình | Triển khai | Ví dụ |
|---|---|---|
| Boundary | Razor View/Web Controller nhận biểu mẫu và WinForms Form | `Booking/Review`, `DoctorEditorForm` |
| Control | API Controller định tuyến và xác thực, Service điều phối nghiệp vụ | `AppointmentsController` → `AppointmentService.BookAsync` |
| Entity | Domain entity; EF Core mapping và DbContext lưu dữ liệu | `Appointment`, `Doctor`, `HospitalDbContext` |
| Kiểm tra dùng chung UC14 | `AppointmentService.ValidateSlotAsync` trong transaction ghi | Đặt và đổi lịch cùng kiểm tra bác sĩ/chuyên khoa, slot và trùng lịch bệnh nhân |
| Kết quả và truy vết | DTO/API response, lịch sử, audit và outbox | Phiếu hẹn, sự kiện đổi khung, thông báo xác nhận |

Sơ đồ phân tích có thể gom trách nhiệm vào lifeline `LichLamViec`; triển khai cần tách quy tắc, mẫu tuần, ca thực tế và khung giờ để hỗ trợ các nhánh trong báo cáo. Kế thừa actor Người dùng không yêu cầu mô hình class phải kế thừa Patient/Doctor/Staff/Admin; hiện sử dụng Role và hồ sơ liên kết.

Chương 5 dành kiểm tra quyền, truy vấn xung đột và giao dịch nhiều bản ghi cho xử lý nghiệp vụ. Vì vậy service giữ trách nhiệm điều phối; D2 sẽ bổ sung phương thức thay đổi trạng thái có ý nghĩa trên entity. A1 không coi mọi phương thức trong sơ đồ là đã có trong code.

## Khác biệt được giữ và cần giải thích

- GUID cho tài khoản/bác sĩ/lịch hẹn và mã hiển thị riêng thay cho kiểu khóa logic trong báo cáo.
- `DateOnly`/`TimeOnly` cho ngày và giờ khám; `ClinicTime` quy đổi theo giờ Việt Nam, audit dùng UTC.
- Lịch hẹn giữ `SpecialtyId` riêng. Đổi chuyên khoa hiện hành của bác sĩ không được viết lại chuyên khoa trên lịch hẹn cũ.
- Hồ sơ bác sĩ và bệnh nhân gắn với tài khoản theo quan hệ duy nhất; vai trò không đồng nghĩa mỗi người được thực hiện mọi thao tác.
- Transaction Serializable, unique index lịch hẹn đang chiếm chỗ và RowVersion bổ sung cơ chế bảo đảm các ràng buộc BR03–BR07.

## Truy vết kiểm chứng A2

| Điểm cần chứng minh | Kiểm thử |
|---|---|
| Tạo bác sĩ với đúng một chuyên khoa, rồi cấp tài khoản | `AdminAndScheduleTests.DoctorProfileExistsBeforeAccountAndBelongsToOneSpecialty` |
| Từ chối thiếu/chuyên khoa không hợp lệ/ngừng hoạt động và payload nhiều chuyên khoa cũ | `SingleSpecialtyTests.DoctorRequiresOneExistingActiveSpecialtyAndRejectsLegacyPayload` |
| Chuyển chuyên khoa chặn lịch Confirmed bị ảnh hưởng; hủy rồi chuyển vẫn giữ lịch sử; đặt mới phải dùng chuyên khoa hiện hành | `SingleSpecialtyTests.ChangingSpecialtyBlocksAffectedBookingsThenPreservesCancelledHistory` |
| Catalog và hồ sơ trả một chuyên khoa, không công bố bác sĩ thuộc chuyên khoa đã ngừng | `SingleSpecialtyTests.CatalogueAndProfileExposeSingleSpecialtyAndHideInactiveDoctors` |
| Migration đi lên/đi xuống giữ dữ liệu và FK | `SingleSpecialtyTests.MigrationBackfillsSingleSpecialtyAndPreservesAppointmentsInBothDirections` |
| Database cũ thiếu hoặc nhiều liên kết bị chặn và không nâng cấp dở dang | `SingleSpecialtyTests.MigrationRejectsMissingOrMultipleLegacySpecialtiesWithoutPartialChanges` |

Danh sách này là điểm kiểm chứng, kết quả chạy cụ thể được ghi trong kế hoạch khi nghiệm thu.
