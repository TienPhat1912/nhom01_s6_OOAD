# Use Case Coverage & Requirement Traceability Matrix

> Ma trận truy vết yêu cầu (Traceability Matrix) từ Nghiệp vụ (Requirements / Use Cases / Business Rules) sang API Endpoints, Màn hình giao diện (UI) và Kịch bản kiểm thử (Test Cases).

> Cập nhật Phase 4, 17/10/2026: backend có 65 tests đạt, Web thêm 6 integration tests; 8 kiểm thử Chromium và smoke WinForms đạt trên API/SQL Server thật. Các màn hình nền tảng đã có được ghi dưới đây; chưa xem toàn bộ UC hoàn thành xuyên suốt sản phẩm. Đặt `HOSPITAL_TEST_SQLSERVER` để chạy đủ 7 test SQL Server. Endpoint và bằng chứng nằm trong `api-documentation.md`, `phase3-verification.md`, `phase4-verification.md`. Các đường dẫn UI chưa triển khai là dự kiến cho Phase 5–6.

---

> Cập nhật Phase 5, 18/10/2026: Patient/Staff/Doctor Web đã hoàn tất trong phạm vi UC bên dưới. 79 .NET tests và 11 browser tests đạt; xem `phase5-verification.md`. Mốc này ghi nhận kết quả Web trước Phase 6.

> Cập nhật Phase 6, 18/10/2026: Windows Forms Admin đã triển khai UC03–04, UC09–12 và UC22–28 qua API thật, cùng tổng quan/nhật ký. 82 .NET tests và 87 assertions WinForms đạt; xem `phase6-verification.md`. Tích hợp toàn hệ thống tiếp tục ở Phase 7.

> Cập nhật Phase 7, 18/10/2026: 102 .NET tests (28 SQL Server), 12 browser tests và 89 assertions WinForms đạt. Chuỗi Web–Desktop thật, cạnh tranh hai browser context, 18 kịch bản bắt buộc và timeout/rollback SQL đã được nghiệm thu; xem `phase7-verification.md`. Phase 8 vẫn chưa hoàn tất.

> Cập nhật Phase 8, 19/10/2026: hoàn tất rà soát giao diện và bàn giao trong phạm vi đồ án/demo. 102 .NET tests (28 SQL), 19 Playwright tests (18 browser + 1 gallery Desktop), 89 + 40 assertions WinForms đạt; 48 màn hình Web và 22 màn hình/form Desktop có ảnh dữ liệu thật. Restart demo giữ dữ liệu. Xem [bằng chứng và giới hạn](phase8-verification.md), [Demo Guide](demo-guide.md) và [giải thích kỹ thuật](technical-explanation.md). Các ghi chú Phase 4–7 phía trên là lịch sử.

## 1. Ma trận truy vết tổng thể (UC01 – UC28)

| Mã UC | Tên Use Case | Tác nhân chính | Business Rules | API Endpoint(s) | Màn hình UI (Web / Desktop) | Test Cases chính | Trạng thái |
|---|---|---|---|---|---|---|---|
| **UC01** | Đăng ký tài khoản | Bệnh nhân | BR01, BR11, BR12 | `POST /api/auth/register` | Patient Web: `/Account/Register` | TC-AUTH-01 (ĐK thành công), TC-AUTH-02 (Trùng username/email) | Web hoàn tất; WebWorkflowTests đăng ký/validation/ép vai trò, browser đăng ký thật |
| **UC02** | Đăng nhập | Tất cả | BR01, BR12 | `POST /api/auth/login` | Web: `/Account/Login`<br>Desktop: LoginForm | TC-AUTH-03 (Login đúng role), TC-AUTH-04 (Sai pass/Khóa) | Đã có Web 4 vai trò và Desktop Admin; kiểm thử phiên/CSRF/phân quyền |
| **UC03** | Cập nhật thông tin cá nhân | Tất cả | BR01, BR11, BR12 | `GET /api/auth/profile`<br>`PUT /api/auth/profile` | Web: `/Account/Profile`<br>Desktop: Hồ sơ cá nhân | TC-PROF-01 (Sửa thông tin hợp lệ), TC-PROF-02 (Validation lỗi) | Web và Desktop đã lưu hồ sơ thật; smoke xác nhận đồng bộ phiên/API |
| **UC04** | Đổi mật khẩu | Tất cả | BR01, BR12 | `POST /api/auth/change-password` | Web: `/Account/ChangePassword`<br>Desktop: Đổi mật khẩu | TC-AUTH-05 (Đổi MK đúng pass cũ), TC-AUTH-06 (Sai pass cũ) | Web/Desktop hoàn tất; validation, đăng nhập lại và thu hồi phiên đã kiểm chứng |
| **UC05** | Tra cứu chuyên khoa | Bệnh nhân, Nhân viên | BR01, BR03, BR11, BR12 | `GET /api/specialties` | Web: `/Catalog/Specialties`<br>Desktop: Chuyên khoa | TC-SPEC-01 (Lấy danh sách active), TC-SPEC-02 (Không hiện inactive) | Web và Desktop có danh sách thật |
| **UC06** | Tra cứu bác sĩ | Bệnh nhân, Nhân viên | BR01, BR03, BR11 | `GET /api/doctors` | Web: `/Catalog`<br>Desktop: Bác sĩ | TC-DOC-01 (Tìm theo tên/CK), TC-DOC-02 (Lọc theo chuyên khoa) | Web tìm tên/mã/chuyên khoa, phân trang, liên kết theo chuyên khoa; browser hồi quy |
| **UC07** | Xem thông tin bác sĩ | Bệnh nhân, Nhân viên | BR01, BR11, BR12 | `GET /api/doctors/{id}` | Web: `/Catalog/Doctor/{id}` | TC-DOC-03 (Xem chi tiết BS & CK), TC-DOC-04 (BS không tồn tại) | Web đã có chi tiết và ngày làm việc từ API |
| **UC08** | Xem khung giờ trống | Bệnh nhân, Nhân viên | BR02, BR03, BR07, BR12 | `GET /api/timeslots/available` | Web: `/Booking/Slots` | TC-SLOT-01 (Chỉ hiện slot Open & chưa qua), TC-SLOT-02 (Ẩn slot đã đặt) | Web tải ngày/giờ khả dụng thật; kiểm thử slot bị chiếm và browser |
| **UC09** | Xem lịch làm việc | Bác sĩ, Quản trị viên | BR01, BR02, BR12 | `GET /api/schedules`<br>`GET /api/schedules/my` | Doctor: `/Schedule`<br>Desktop: Lịch làm việc | TC-SCHED-01 (BS chỉ thấy lịch của mình), TC-SCHED-02 (Admin thấy all) | Doctor có lịch 7 ngày; Admin Desktop lọc khoảng ngày/bác sĩ, xem ca và khung giờ |
| **UC10** | Tạo lịch làm việc | Quản trị viên | BR02, BR03, BR11, BR12 | `POST /api/schedules` | Desktop: Thêm lịch làm việc | TC-SCHED-03 (Tạo lịch + auto sinh slots), TC-SCHED-04 (Chống trùng giờ cùng BS) | Desktop đã triển khai qua API; smoke SQL Server xác nhận nghiệp vụ |
| **UC11** | Điều chỉnh lịch làm việc | Quản trị viên | BR02, BR03, BR09, BR11, BR12 | `PUT /api/schedules/{id}` | Desktop: Sửa lịch làm việc | TC-SCHED-05 (Sửa khi chưa có lịch hẹn), TC-SCHED-06 (Chặn sửa nếu đã có Confirmed) | Desktop đã triển khai qua API; smoke SQL Server xác nhận nghiệp vụ |
| **UC12** | Đóng khung giờ | Quản trị viên | BR02, BR03, BR09, BR11, BR12 | `POST /api/timeslots/{id}/close` | Desktop: Chi tiết lịch LV / Đóng slot | TC-SLOT-03 (Đóng slot trống), TC-SLOT-04 (Chặn đóng slot có lịch Confirmed) | Desktop đã triển khai qua API; smoke SQL Server xác nhận nghiệp vụ |
| **UC13** | Đặt lịch khám | Bệnh nhân, Nhân viên | BR01, BR03-05, BR07, BR10-12 | `POST /api/appointments` | Web: `/Booking/Start`, `/Booking/Review`, `/Booking/Receipt/{id}`; Staff: `/Patients` | TC-APPT-01 (Đặt thành công -> Confirmed), TC-APPT-02 (NV đặt hộ ghi nhận BookedBy) | Web hoàn tất; RequestKey giữ nguyên khi thử lại; WebWorkflowTests SQLite/SQL Server và browser |
| **UC14** | Kiểm tra điều kiện đặt | Dùng chung (UC13, UC17) | BR03, BR04, BR07, BR12 | (Nội bộ Application Service) | Tích hợp trong flow Đặt/Đổi | TC-VAL-01 (Chặn slot quá khứ), TC-VAL-02 (Chặn BN trùng lịch), TC-VAL-03 (Slot kín) | Tích hợp Web/API; slot không trống, chồng lấn, hết hạn, xung đột; backend và WebWorkflowTests |
| **UC15** | Xem lịch hẹn | Bệnh nhân, Bác sĩ, Nhân viên | BR01, BR05, BR06, BR12 | `GET /api/appointments/my`<br>`GET /api/appointments/doctor`<br>`GET /api/appointments` | Web: `/Appointments`, `/Appointments/Detail/{id}` | TC-APPT-03 (BN chỉ thấy lịch mình), TC-APPT-04 (BS chỉ thấy lịch của mình) | Web danh sách/chi tiết theo quyền; bộ lọc ngày/trạng thái/thứ tự, lịch sử; browser và test phạm vi |
| **UC16** | Tra cứu lịch hẹn | Nhân viên | BR01, BR05, BR06, BR08 | `GET /api/appointments/search` | Staff: `/Appointments` | TC-APPT-05 (Tìm theo mã LH, SĐT, Tên BN, Ngày) | Staff tìm mã/tên/điện thoại, ngày, trạng thái, bác sĩ; browser tìm đúng bệnh nhân/lịch |
| **UC17** | Đổi lịch hẹn | Bệnh nhân, Nhân viên | BR01, BR03-04, BR06-07, BR10-12 | `POST /api/appointments/{id}/reschedule` | Web: `/Booking/Start?appointmentId={id}` | TC-APPT-06 (BN đổi trước >= 2h), TC-APPT-07 (BN đổi < 2h bị chặn), TC-APPT-08 (NV đổi trước giờ khám + có lý do) | Web hoàn tất; bản đổi giữ RowVersion ban đầu; kiểm thử xung đột, hạn đổi, lý do Staff |
| **UC18** | Hủy lịch hẹn | Bệnh nhân, Nhân viên | BR01, BR05-07, BR10-12 | `POST /api/appointments/{id}/cancel` | Web: `/Reception/Cancel/{id}` | TC-APPT-09 (BN hủy >= 2h -> giải phóng slot), TC-APPT-10 (Chặn hủy lịch < 2h với BN), TC-APPT-11 (NV hủy kèm lý do) | Web hoàn tất; lý do Staff, hạn 2 giờ và RowVersion; integration + browser |
| **UC19** | Tiếp nhận bệnh nhân | Nhân viên | BR01, BR05, BR08, BR11, BR12 | `POST /api/appointments/{id}/check-in` | Staff: `/Reception/CheckIn/{id}` | TC-CHECKIN-01 (Tiếp nhận lịch Confirmed -> CheckedIn), TC-CHECKIN-02 (Chặn tiếp nhận lịch Cancelled/NoShow) | Web hoàn tất; checkbox đối chiếu bắt buộc, kiểm tra ngày/giờ/trạng thái; integration + browser |
| **UC20** | Ghi nhận bệnh nhân không đến | Nhân viên | BR01, BR05, BR08, BR11, BR12 | `POST /api/appointments/{id}/no-show` | Staff: `/Reception/NoShow/{id}` | TC-NOSHOW-01 (Ghi nhận NoShow sau khi hết slot), TC-NOSHOW-02 (Chặn ghi NoShow khi chưa hết giờ) | Web hoàn tất; kiểm thử MVC/API với đồng hồ trước/sau giờ kết thúc; chỉ Staff |
| **UC21** | Xem thông báo | Bệnh nhân | BR01, BR05, BR10, BR12 | `GET /api/notifications`<br>`PUT /api/notifications/{id}/read` | Patient: `/Notifications` | TC-NOTIF-01 (Hiện thông báo sau đặt/đổi/hủy), TC-NOTIF-02 (Đánh dấu đã đọc) | Web có danh sách/đánh dấu đã đọc; Phase 7 kiểm chứng đặt–đổi–hủy tạo đúng ba thông báo, SQL đối chiếu trạng thái đọc |
| **UC22** | Cấp tài khoản | Quản trị viên | BR01, BR11, BR12 | `POST /api/admin/users` | Desktop Admin: Quản lý người dùng (Thêm) | TC-ADMIN-01 (Tạo TK Staff/Doctor/Admin), TC-ADMIN-02 (Chặn trùng username/email) | Desktop đã triển khai qua API; smoke SQL Server xác nhận nghiệp vụ |
| **UC23** | Cập nhật tài khoản | Quản trị viên | BR01, BR11, BR12 | `PUT /api/admin/users/{id}` | Desktop Admin: Sửa tài khoản | TC-ADMIN-03 (Cập nhật thông tin/khóa tài khoản), TC-ADMIN-04 (Chặn khóa Admin cuối cùng) | Desktop đã triển khai qua API; smoke SQL Server xác nhận nghiệp vụ |
| **UC24** | Phân quyền tài khoản | Quản trị viên | BR01, BR11, BR12 | `PUT /api/admin/users/{id}/role` | Desktop Admin: Phân quyền vai trò | TC-ADMIN-05 (Đổi vai trò tài khoản), TC-ADMIN-06 (Chặn tước quyền Admin cuối cùng) | Desktop đã triển khai qua API; smoke SQL Server xác nhận nghiệp vụ |
| **UC25** | Thêm bác sĩ | Quản trị viên | BR01, BR03, BR11, BR12 | `POST /api/admin/doctors` | Desktop Admin: Quản lý Bác sĩ (Thêm) | TC-ADMIN-07 (Thêm BS + gán CK), TC-ADMIN-08 (Mã BS tự sinh duy nhất) | Desktop đã triển khai qua API; smoke SQL Server xác nhận nghiệp vụ |
| **UC26** | Cập nhật bác sĩ | Quản trị viên | BR01, BR09, BR11, BR12 | `PUT /api/admin/doctors/{id}` | Desktop Admin: Sửa Bác sĩ | TC-ADMIN-09 (Sửa thông tin BS), TC-ADMIN-10 (Ngừng hoạt động BS -> giữ liên kết) | Desktop đã triển khai qua API; smoke SQL Server xác nhận nghiệp vụ |
| **UC27** | Thêm chuyên khoa | Quản trị viên | BR01, BR11, BR12 | `POST /api/admin/specialties` | Desktop Admin: Quản lý Chuyên khoa (Thêm) | TC-ADMIN-11 (Thêm CK mới), TC-ADMIN-12 (Trùng mã/tên CK) | Desktop đã triển khai qua API; smoke SQL Server xác nhận nghiệp vụ |
| **UC28** | Cập nhật chuyên khoa | Quản trị viên | BR01, BR09, BR11, BR12 | `PUT /api/admin/specialties/{id}` | Desktop Admin: Sửa Chuyên khoa | TC-ADMIN-13 (Sửa tên/mô tả CK), TC-ADMIN-14 (Ngừng hoạt động CK) | Desktop đã triển khai qua API; smoke SQL Server xác nhận nghiệp vụ |

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
| 17 | Xử lý khi mất kết nối database / transaction timeout | Toàn hệ thống | Lỗi SQL Server trả 503 `DATABASE_UNAVAILABLE` kèm trace, kể cả sau khi hết lượt retry; lỗi khác trả 500. Không lưu dữ liệu dở dang |
| 18 | Tự động tạo bản ghi thông báo khi đặt/đổi/hủy thành công | UC13, UC17, UC18, BR10 | Bản ghi thông báo xuất hiện trong `Notifications`, trạng thái `IsRead = false` |

## 4. Kết quả nghiệm thu tích hợp Phase 7

Toàn bộ 18 dòng ở mục 3 đã chạy thành công trên SQL Server qua `Phase7AcceptanceTests.RequiredScenario`, với số case tương ứng STT. Xem bảng bằng chứng chi tiết trong [kiểm chứng Phase 7](phase7-verification.md). Cạnh tranh còn được chạy từ hai browser context trong `tests/browser/phase7.spec.js`; các request thật cùng đi qua MVC/API/SQL Server, đúng một người nhận phiếu hẹn.

Kiểm tra bổ sung: `SqlServerWriteFailureRollsBackMutationAndOutboxDeliveryCanRecover` xác nhận rollback và outbox phục hồi; `SqlServerTimeoutTests.CommandTimeoutAfterWriteRollsBackBothSlotsHistoryAuditAndOutbox` xác nhận timeout SQL sau một lệnh ghi không để lại cập nhật dở dang. Runner `scripts/verify-phase7.ps1` nối các form Desktop thật với browser, đối chiếu trạng thái SQL và dọn database riêng. Kết quả toàn bộ: 102 .NET tests, 12 browser tests, 89 assertions WinForms, không bỏ qua nhóm SQL khi nghiệm thu.
