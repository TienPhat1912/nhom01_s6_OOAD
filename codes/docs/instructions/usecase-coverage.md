# Use Case Coverage & Requirement Traceability Matrix

> Ma trận truy vết yêu cầu (Traceability Matrix) từ Nghiệp vụ (Requirements / Use Cases / Business Rules) sang API Endpoints, Màn hình giao diện (UI) và Kịch bản kiểm thử (Test Cases).

> Cập nhật Phase 3, 16/10/2026: backend đã có API/service cho UC01–UC28. UI chưa triển khai, vì vậy chưa xem các UC là hoàn thành xuyên suốt sản phẩm. Các test thực tế nằm trong `Hospital.UnitTests`, `Hospital.IntegrationTests`; 4 test SQL Server được để Skip khi chưa có cấu hình server. Endpoint đang dùng và kết quả kiểm chứng nằm trong `api-documentation.md`, `phase3-verification.md`.

---

## 1. Ma trận truy vết tổng thể (UC01 – UC28)

| Mã UC | Tên Use Case | Tác nhân chính | Business Rules | API Endpoint(s) | Màn hình UI (Web / Desktop) | Test Cases chính | Trạng thái |
|---|---|---|---|---|---|---|---|
| **UC01** | Đăng ký tài khoản | Bệnh nhân | BR01, BR11, BR12 | `POST /api/auth/register` | Patient Web: `/register` | TC-AUTH-01 (ĐK thành công), TC-AUTH-02 (Trùng username/email) | Backend đã có; UI chưa có |
| **UC02** | Đăng nhập | Tất cả | BR01, BR12 | `POST /api/auth/login` | Web: `/login`<br>Desktop: Form Login | TC-AUTH-03 (Login đúng role), TC-AUTH-04 (Sai pass/Khóa) | Backend đã có; UI chưa có |
| **UC03** | Cập nhật thông tin cá nhân | Tất cả | BR01, BR11, BR12 | `GET /api/auth/profile`<br>`PUT /api/auth/profile` | Web: `/profile`<br>Desktop: Profile Dialog | TC-PROF-01 (Sửa thông tin hợp lệ), TC-PROF-02 (Validation lỗi) | Backend đã có; UI chưa có |
| **UC04** | Đổi mật khẩu | Tất cả | BR01, BR12 | `POST /api/auth/change-password` | Web: `/change-password`<br>Desktop: Change Pass Dialog | TC-AUTH-05 (Đổi MK đúng pass cũ), TC-AUTH-06 (Sai pass cũ) | Backend đã có; UI chưa có |
| **UC05** | Tra cứu chuyên khoa | Bệnh nhân, Nhân viên | BR01, BR03, BR11, BR12 | `GET /api/specialties` | Patient Web: `/specialties`<br>Staff Portal: Tra cứu | TC-SPEC-01 (Lấy danh sách active), TC-SPEC-02 (Không hiện inactive) | Backend đã có; UI chưa có |
| **UC06** | Tra cứu bác sĩ | Bệnh nhân, Nhân viên | BR01, BR03, BR11 | `GET /api/doctors` | Patient Web: `/doctors`<br>Staff Portal: Tìm kiếm BS | TC-DOC-01 (Tìm theo tên/CK), TC-DOC-02 (Lọc theo chuyên khoa) | Backend đã có; UI chưa có |
| **UC07** | Xem thông tin bác sĩ | Bệnh nhân, Nhân viên | BR01, BR11, BR12 | `GET /api/doctors/{id}` | Patient Web: `/doctors/{id}` | TC-DOC-03 (Xem chi tiết BS & CK), TC-DOC-04 (BS không tồn tại) | Backend đã có; UI chưa có |
| **UC08** | Xem khung giờ trống | Bệnh nhân, Nhân viên | BR02, BR03, BR07, BR12 | `GET /api/timeslots/available` | Patient Web: Booking step 4<br>Staff Portal: Slot selector | TC-SLOT-01 (Chỉ hiện slot Open & chưa qua), TC-SLOT-02 (Ẩn slot đã đặt) | Backend đã có; UI chưa có |
| **UC09** | Xem lịch làm việc | Bác sĩ, Quản trị viên | BR01, BR02, BR12 | `GET /api/schedules`<br>`GET /api/schedules/my` | Doctor Portal: `/doctor/schedule`<br>Desktop: Quản lý lịch LV | TC-SCHED-01 (BS chỉ thấy lịch của mình), TC-SCHED-02 (Admin thấy all) | Backend đã có; UI chưa có |
| **UC10** | Tạo lịch làm việc | Quản trị viên | BR02, BR03, BR11, BR12 | `POST /api/schedules` | Desktop: Thêm lịch làm việc | TC-SCHED-03 (Tạo lịch + auto sinh slots), TC-SCHED-04 (Chống trùng giờ cùng BS) | Backend đã có; UI chưa có |
| **UC11** | Điều chỉnh lịch làm việc | Quản trị viên | BR02, BR03, BR09, BR11, BR12 | `PUT /api/schedules/{id}` | Desktop: Sửa lịch làm việc | TC-SCHED-05 (Sửa khi chưa có lịch hẹn), TC-SCHED-06 (Chặn sửa nếu đã có Confirmed) | Backend đã có; UI chưa có |
| **UC12** | Đóng khung giờ | Quản trị viên | BR02, BR03, BR09, BR11, BR12 | `POST /api/timeslots/{id}/close` | Desktop: Chi tiết lịch LV / Đóng slot | TC-SLOT-03 (Đóng slot trống), TC-SLOT-04 (Chặn đóng slot có lịch Confirmed) | Backend đã có; UI chưa có |
| **UC13** | Đặt lịch khám | Bệnh nhân, Nhân viên | BR01, BR03-05, BR07, BR10-12 | `POST /api/appointments` | Patient Web: `/booking`<br>Staff Portal: `/staff/booking` | TC-APPT-01 (Đặt thành công -> Confirmed), TC-APPT-02 (NV đặt hộ ghi nhận BookedBy) | Backend đã có; UI chưa có |
| **UC14** | Kiểm tra điều kiện đặt | Dùng chung (UC13, UC17) | BR03, BR04, BR07, BR12 | (Nội bộ Application Service) | Tích hợp trong flow Đặt/Đổi | TC-VAL-01 (Chặn slot quá khứ), TC-VAL-02 (Chặn BN trùng lịch), TC-VAL-03 (Slot kín) | Backend đã có; UI chưa có |
| **UC15** | Xem lịch hẹn | Bệnh nhân, Bác sĩ, Nhân viên | BR01, BR05, BR06, BR12 | `GET /api/appointments/my`<br>`GET /api/appointments/doctor`<br>`GET /api/appointments` | Patient Web: `/my-appointments`<br>Doctor: `/doctor/appointments`<br>Staff: `/staff/appointments` | TC-APPT-03 (BN chỉ thấy lịch mình), TC-APPT-04 (BS chỉ thấy lịch của mình) | Backend đã có; UI chưa có |
| **UC16** | Tra cứu lịch hẹn | Nhân viên | BR01, BR05, BR06, BR08 | `GET /api/appointments/search` | Staff Portal: Tra cứu tiếp đón | TC-APPT-05 (Tìm theo mã LH, SĐT, Tên BN, Ngày) | Backend đã có; UI chưa có |
| **UC17** | Đổi lịch hẹn | Bệnh nhân, Nhân viên | BR01, BR03-04, BR06-07, BR10-12 | `POST /api/appointments/{id}/reschedule` | Patient Web: Modal đổi lịch<br>Staff Portal: Action đổi lịch | TC-APPT-06 (BN đổi trước >= 2h), TC-APPT-07 (BN đổi < 2h bị chặn), TC-APPT-08 (NV đổi trước giờ khám + có lý do) | Backend đã có; UI chưa có |
| **UC18** | Hủy lịch hẹn | Bệnh nhân, Nhân viên | BR01, BR05-07, BR10-12 | `POST /api/appointments/{id}/cancel` | Patient Web: Modal hủy lịch<br>Staff Portal: Action hủy lịch | TC-APPT-09 (BN hủy >= 2h -> giải phóng slot), TC-APPT-10 (Chặn hủy lịch < 2h với BN), TC-APPT-11 (NV hủy kèm lý do) | Backend đã có; UI chưa có |
| **UC19** | Tiếp nhận bệnh nhân | Nhân viên | BR01, BR05, BR08, BR11, BR12 | `POST /api/appointments/{id}/check-in` | Staff Portal: Nút "Tiếp nhận" | TC-CHECKIN-01 (Tiếp nhận lịch Confirmed -> CheckedIn), TC-CHECKIN-02 (Chặn tiếp nhận lịch Cancelled/NoShow) | Backend đã có; UI chưa có |
| **UC20** | Ghi nhận bệnh nhân không đến | Nhân viên | BR01, BR05, BR08, BR11, BR12 | `POST /api/appointments/{id}/no-show` | Staff Portal: Nút "Ghi nhận vắng" | TC-NOSHOW-01 (Ghi nhận NoShow sau khi hết slot), TC-NOSHOW-02 (Chặn ghi NoShow khi chưa hết giờ) | Backend đã có; UI chưa có |
| **UC21** | Xem thông báo | Bệnh nhân | BR01, BR05, BR10, BR12 | `GET /api/notifications`<br>`PUT /api/notifications/{id}/read` | Patient Web: Dropdown/Trang thông báo | TC-NOTIF-01 (Hiện thông báo sau đặt/đổi/hủy), TC-NOTIF-02 (Đánh dấu đã đọc) | Backend đã có; UI chưa có |
| **UC22** | Cấp tài khoản | Quản trị viên | BR01, BR11, BR12 | `POST /api/admin/users` | Desktop Admin: Quản lý người dùng (Thêm) | TC-ADMIN-01 (Tạo TK Staff/Doctor/Admin), TC-ADMIN-02 (Chặn trùng username/email) | Backend đã có; UI chưa có |
| **UC23** | Cập nhật tài khoản | Quản trị viên | BR01, BR11, BR12 | `PUT /api/admin/users/{id}` | Desktop Admin: Sửa tài khoản | TC-ADMIN-03 (Cập nhật thông tin/khóa tài khoản), TC-ADMIN-04 (Chặn khóa Admin cuối cùng) | Backend đã có; UI chưa có |
| **UC24** | Phân quyền tài khoản | Quản trị viên | `PUT /api/admin/users/{id}/role` | Desktop Admin: Phân quyền vai trò | TC-ADMIN-05 (Đổi vai trò tài khoản), TC-ADMIN-06 (Chặn tước quyền Admin cuối cùng) | Backend đã có; UI chưa có |
| **UC25** | Thêm bác sĩ | Quản trị viên | BR01, BR03, BR11, BR12 | `POST /api/admin/doctors` | Desktop Admin: Quản lý Bác sĩ (Thêm) | TC-ADMIN-07 (Thêm BS + gán CK), TC-ADMIN-08 (Mã BS tự sinh duy nhất) | Backend đã có; UI chưa có |
| **UC26** | Cập nhật bác sĩ | Quản trị viên | BR01, BR09, BR11, BR12 | `PUT /api/admin/doctors/{id}` | Desktop Admin: Sửa Bác sĩ | TC-ADMIN-09 (Sửa thông tin BS), TC-ADMIN-10 (Ngừng hoạt động BS -> giữ liên kết) | Backend đã có; UI chưa có |
| **UC27** | Thêm chuyên khoa | Quản trị viên | BR01, BR11, BR12 | `POST /api/admin/specialties` | Desktop Admin: Quản lý Chuyên khoa (Thêm) | TC-ADMIN-11 (Thêm CK mới), TC-ADMIN-12 (Trùng mã/tên CK) | Backend đã có; UI chưa có |
| **UC28** | Cập nhật chuyên khoa | Quản trị viên | BR01, BR09, BR11, BR12 | `PUT /api/admin/specialties/{id}` | Desktop Admin: Sửa Chuyên khoa | TC-ADMIN-13 (Sửa tên/mô tả CK), TC-ADMIN-14 (Ngừng hoạt động CK) | Backend đã có; UI chưa có |

---

## 2. Ma trận chi tiết Quy tắc nghiệp vụ (BR01 – BR12)

| Mã BR | Tên Quy tắc | Use Cases áp dụng | Điểm kiểm tra ràng buộc (Constraint & Validation) |
|---|---|---|---|
| **BR01** | Phân quyền & Quản lý tài khoản | UC01, UC02, UC03, UC04, UC13, UC15, UC16, UC17, UC18, UC19, UC20, UC22-28 | - Backend JWT Role Claim & Authorize Attributes.<br>- Resource-based check: `PatientId == CurrentUser.PatientId`.<br>- Bác sĩ: `DoctorId == CurrentUser.DoctorId`.<br>- Staff/Admin: Kiểm tra vai trò hệ thống. |
| **BR02** | Quản lý lịch làm việc | UC08, UC09, UC10, UC11, UC12 | - Chỉ Admin mới có quyền POST/PUT/DELETE schedules & close slots.<br>- Bác sĩ chỉ có quyền đọc lịch của mình.<br>- Nhân viên chỉ tra cứu slot khả dụng. |
| **BR03** | Khung giờ khả dụng & Chống trùng | UC08, UC10, UC11, UC12, UC13, UC14, UC17, UC25 | - 1 TimeSlot chỉ có tối đa 1 lịch Confirmed/CheckedIn.<br>- `Status == Open` và `SlotDate + StartTime > DateTime.UtcNow`.<br>- Unique index `(DoctorId, WorkDate, StartTime)` chống trùng ca trực của BS. |
| **BR04** | Chống trùng lịch khám cho Bệnh nhân | UC13, UC14, UC17 | - Kiểm tra xem BN đã có lịch hẹn nào khác đang ở trạng thái `Confirmed` trong cùng khung giờ hoặc có khoảng thời gian giao nhau.<br>- Khi đổi lịch (UC17), loại trừ AppointmentId hiện tại khỏi phạm vi đối chiếu. |
| **BR05** | Trạng thái vòng đời lịch hẹn | UC13, UC15, UC16, UC17, UC18, UC19, UC20, UC21 | - Trạng thái: `Confirmed (0)`, `Cancelled (1)`, `CheckedIn (2)`, `NoShow (3)`.<br>- Đặt lịch hợp lệ chuyển ngay sang `Confirmed`.<br>- Mỗi lần đổi trạng thái phải lưu vào `AppointmentStatusHistory`. |
| **BR06** | Điều kiện đổi và hủy lịch | UC15, UC16, UC17, UC18 | - Bệnh nhân: chỉ đổi/hủy lịch `Confirmed` và thời gian bắt đầu khám còn cách hiện tại $\ge 2$ giờ.<br>- Nhân viên: đổi/hủy trước thời điểm bắt đầu khám, bắt buộc nhập lý do (`Reason`). |
| **BR07** | Tính nguyên tử & Chống trùng yêu cầu | UC08, UC13, UC14, UC17, UC18 | - Xử lý trong Database Transaction với mức cách ly an toàn (`SERIALIZABLE` hoặc Optimistic Concurrency với `RowVersion`).<br>- Đổi lịch thất bại phải rollback hoàn toàn, giữ nguyên slot cũ.<br>- Chống submit lặp (Idempotency token / concurrency check). |
| **BR08** | Tiếp nhận & Ghi nhận không đến | UC16, UC19, UC20 | - Tiếp nhận: chỉ thực hiện khi trạng thái là `Confirmed` và đúng ngày/giờ khám.<br>- Không đến: chỉ ghi nhận khi khung giờ đã kết thúc (`SlotDate + EndTime < Now`) và lịch vẫn ở trạng thái `Confirmed`. |
| **BR09** | Ảnh hưởng khi thay đổi lịch làm việc | UC11, UC12, UC26, UC28 | - Chặn đóng hoặc thay đổi lịch làm việc nếu bên trong chứa khung giờ đã có lịch hẹn `Confirmed`.<br>- Yêu cầu nhân viên/bệnh nhân đổi hoặc hủy lịch hẹn trước khi đóng slot/lịch làm việc. |
| **BR10** | Thông báo & Nhắc lịch | UC13, UC17, UC18, UC21 | - Tự động tạo bản ghi `Notification` khi đặt, đổi hoặc hủy thành công.<br>- Background job quét và gửi nhắc lịch trước 1 giờ.<br>- Ngoại lệ: Lịch đặt sát giờ (< 1 giờ) không sinh thêm nhắc lịch thừa.<br>- Lỗi ghi notification không được rollback lịch hẹn chính. |
| **BR11** | Toàn vẹn dữ liệu & Truy vết (Audit) | UC01, UC03, UC10, UC11, UC13, UC17, UC18, UC19, UC20, UC22-28 | - Mã định danh unique: `PatientCode`, `DoctorCode`, `SpecialtyCode`, `AppointmentCode`.<br>- Ghi `AuditLog` cho các hành động thay đổi dữ liệu.<br>- Soft delete / Inactive flag (`IsActive = 0`) cho danh mục đã có lịch sử.<br>- Chặn vô hiệu hóa tài khoản Quản trị viên hoạt động cuối cùng. |
| **BR12** | An toàn thông tin & Toàn vẹn phiên | Toàn bộ hệ thống | - Hash mật khẩu chuẩn bằng thuật toán an toàn (BCrypt / PBKDF2 ASP.NET Identity).<br>- Token JWT có thời hạn, xác thực mọi request.<br>- Kiểm tra trạng thái tài khoản `IsActive == true` trước khi cho phép đăng nhập/thao tác. |

---

## 3. Danh sách kiểm thử bắt buộc (18 Kịch bản quy định)

| STT | Kịch bản kiểm thử | Nhóm Use Case | Trạng thái mong đợi |
|---|---|---|---|
| 1 | Đăng ký trùng username / email | UC01 | Báo lỗi 409 Conflict, yêu cầu chọn thông tin khác |
| 2 | Đăng nhập sai mật khẩu / tài khoản bị khóa | UC02 | Báo lỗi 401 Unauthorized, không tiết lộ user có tồn tại hay không |
| 3 | Truy cập API trái quyền (VD: Patient gọi API Admin) | BR01 | Báo lỗi 403 Forbidden |
| 4 | Bệnh nhân cố tình xem/sửa lịch của bệnh nhân khác | UC15, UC17, UC18 | Báo lỗi 403 Forbidden hoặc 404 Not Found |
| 5 | Đặt lịch vào khung giờ đã kín (Occupied) | UC13, UC14 | Báo lỗi 400 Bad Request / 409 Conflict: Khung giờ không khả dụng |
| 6 | Hai bệnh nhân đặt cùng 1 khung giờ đồng thời (Race condition) | UC13, BR07 | Đúng 1 bệnh nhân thành công, bệnh nhân còn lại nhận thông báo lỗi slot đã có người đặt |
| 7 | Một bệnh nhân đặt 2 lịch hẹn chồng lấn thời gian | UC13, UC14, BR04 | Báo lỗi 400 Bad Request: Bệnh nhân đã có lịch hẹn trùng thời gian |
| 8 | Bệnh nhân đổi lịch trước giờ khám < 2 giờ | UC17, BR06 | Báo lỗi 400 Bad Request: Đã quá thời hạn cho phép tự đổi lịch |
| 9 | Đổi lịch thất bại (do slot mới bị chiếm) | UC17, BR07 | Rollback: Giữ nguyên lịch hẹn và slot cũ của bệnh nhân |
| 10 | Hủy lịch thành công | UC18, BR06 | Khung giờ cũ được giải phóng chuyển về `Open`, trạng thái lịch thành `Cancelled` |
| 11 | Nhân viên tiếp nhận lịch không hợp lệ (đã hủy hoặc quá hạn) | UC19, BR08 | Báo lỗi: Chỉ tiếp nhận lịch hẹn đang ở trạng thái Đã xác nhận |
| 12 | Ghi nhận bệnh nhân không đến khi khung giờ chưa kết thúc | UC20, BR08 | Báo lỗi: Chưa hết khung giờ khám, không thể ghi nhận vắng mặt |
| 13 | Quản trị viên cố đóng khung giờ đang có lịch Confirmed | UC12, BR09 | Báo lỗi: Khung giờ đang có lịch hẹn đã xác nhận, không thể đóng |
| 14 | Ngừng hoạt động bác sĩ/chuyên khoa đã có lịch sử khám | UC26, UC28, BR11 | Cập nhật `IsActive = false`, giữ nguyên dữ liệu lịch sử không lỗi foreign key |
| 15 | Quản trị viên cố vô hiệu hóa tài khoản Admin hoạt động cuối cùng | UC23, UC24, BR11 | Báo lỗi: Không thể khóa hoặc tước quyền của quản trị viên hoạt động duy nhất |
| 16 | Gửi lại yêu cầu đặt lịch đã thành công (Re-submit / Idempotency) | UC13, BR07 | Không tạo lịch hẹn thứ hai, trả về kết quả hợp lệ hoặc báo trùng |
| 17 | Xử lý khi mất kết nối database / transaction timeout | Toàn hệ thống | Trả về mã lỗi 500 kèm mã trace id, không lưu dữ liệu chắp vá (Atomic) |
| 18 | Tự động tạo bản ghi thông báo khi đặt/đổi/hủy thành công | UC13, UC17, UC18, BR10 | Bản ghi thông báo xuất hiện trong `Notifications`, trạng thái `IsRead = false` |
