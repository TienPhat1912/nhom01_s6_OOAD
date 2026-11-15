# Giải thích kỹ thuật để học và bảo vệ đồ án

## 1. Hai giao diện dùng chung dữ liệu

Web MVC và WinForms dùng `Hospital.Client` gửi HTTP tới cùng API. Desktop tạo WorkingSchedule, API lưu SQL và sinh TimeSlots; Patient đọc API sẽ thấy chính các Id đó. Không sao chép database cho mỗi giao diện. Cần tải lại vì chưa có push realtime.

Đọc mã: `HospitalApiClient`, `PortalApi`, `AdminSession`, các controller API và `ScheduleService`.

## 2. OOP, interface và dependency injection

`IAppointmentService` là hợp đồng; `AppointmentService` hiện thực và được DI truyền DbContext, actor, clock, dispatcher, logger. Controller phụ thuộc interface. Test có thể thay TimeProvider nhưng vẫn chạy service thực.

`EditorForm` tái sử dụng bố cục, DataAnnotations, xác nhận, lưu và xử lý lỗi. Form con ghi đè `CreateRequest`/`PersistAsync`. `IDesktopDialogs` cho test tự trả lời xác nhận, còn sản phẩm dùng dialog WinForms thật. DTO tách dữ liệu mạng khỏi entity được EF tracking; không trả PasswordHash cho giao diện.

## 3. Async/await không thay thế transaction

HTTP/SQL chờ I/O bằng `await`; UI vẫn phản hồi và thể hiện loading. Event handler WinForms quay lại message loop để cập nhật controls. Smoke bật kiểm tra cross-thread.

Async không tự ngăn hai request cùng đọc slot trống. Việc bảo vệ dữ liệu nằm ở transaction, index và kiểm tra trong service.

## 4. Hai người cùng đặt một slot

A và B đều thấy 08:30 còn trống. Mỗi thao tác xác nhận chạy trong transaction Serializable. Service đọc lại slot và lịch chồng lấn rồi ghi Appointment/slot/history/audit/outbox. Unique index `TimeSlotId WHERE Status IN (0,2)` chỉ cho một lịch còn hiệu lực trên slot. Người còn lại nhận lỗi khả dụng/concurrency.

Nếu SQL chọn transaction làm deadlock victim, execution strategy có thể retry; code xóa tracking và đọc lại trạng thái. Không giữ entity đã sửa của lượt thất bại. Bằng chứng nằm trong test SQL và cuộc đua hai browser context ở Phase 7/8.

## 5. RowVersion và RequestKey

| Cơ chế | Giải quyết | Ví dụ |
|---|---|---|
| RowVersion | Sửa bản dữ liệu đã cũ | Staff vừa tiếp nhận, Patient còn trang hủy cũ: phải tải lại |
| RequestKey + fingerprint | Gửi lặp cùng yêu cầu | Đặt đã commit nhưng mất phản hồi: gửi key cũ nhận cùng lịch |

RowVersion là 8 byte do SQL quản lý, không phải ngày giờ và không do client tăng. Key mới chỉ dành cho ý định đặt mới; fingerprint chặn một key dùng cho hai nội dung. Web bảo vệ draft bằng Data Protection và gắn ActorId để không sửa tùy ý patient/slot/RowVersion trong bản xác nhận.

## 6. Đổi lịch phải nguyên tử

Giải phóng slot cũ, chiếm slot mới, sửa Appointment, history/audit/outbox phải cùng commit hoặc cùng rollback. Lưu từng bảng độc lập có thể làm mất chỗ cũ hoặc giữ hai chỗ.

Test timeout thực hiện một UPDATE trong transaction rồi gây timeout SQL thật; đọc context mới xác nhận cả slot, RowVersion, lịch sử, audit/outbox không ghi dở. UI hiện lỗi chưa đủ để kết luận rollback.

## 7. Outbox bảo vệ thông báo

Outbox lưu ý định gửi cùng transaction lịch hẹn. Dispatcher/worker giao sau commit; lỗi giao không đảo ngược lịch đã đặt. Unique OutboxId giúp xử lý lại không tạo trùng. Hệ thống chỉ có thông báo trong ứng dụng, chưa tích hợp email/SMS.

## 8. Xác thực, quyền và quyền sở hữu

BCrypt hash mật khẩu với work factor 11, không giải mã ngược. API kiểm tra TokenVersion/role/trạng thái hiện tại ngoài chữ ký JWT. Đổi mật khẩu/quyền làm phiên cũ hết hiệu lực.

`[Authorize]` kiểm tra vai trò; truy vấn/service kiểm tra chủ sở hữu. Patient không dùng Id của người khác để xem/sửa/đặt lịch. Backend trả 404 với tài nguyên ngoài phạm vi để không tiết lộ lịch người khác.

MVC dùng cookie nên cần antiforgery cho POST. Desktop dùng bearer token trong bộ nhớ. Ẩn nút chỉ hỗ trợ UX; backend vẫn từ chối request trái quyền.

## 9. EF Core, LINQ và lịch sử

LINQ áp dụng filter/paging trên server; `AsNoTracking` cho dữ liệu đọc, `Include` tải quan hệ cần map DTO. Migration lưu lịch sử schema.

Cancelled/NoShow không xóa Appointment. Slot có nhiều lịch sử nhưng tối đa một Confirmed/CheckedIn. Danh mục ngừng hoạt động dùng IsActive. Hạn hai giờ, ngày tiếp nhận, ca 30 phút và chồng lấn được kiểm tra trong service; không phải tất cả là CHECK constraint SQL.

## 10. Trình bày bằng chứng đúng phạm vi

Dùng [ma trận UC/BR](usecase-coverage.md), TRX, log/ảnh và SQL trong [Phase 8](phase8-verification.md). Phân biệt test logic, SQL, browser và review ảnh. WinForms chạy ẩn không phải kiểm thử thủ công đa DPI; cuộc đua hai request không phải thử tải lớn; CheckedIn không phải hoàn tất khám.
