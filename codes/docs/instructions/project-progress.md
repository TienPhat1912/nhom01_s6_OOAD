# Kế hoạch & Tiến độ Dự án — Hospital Appointment System

> Tài liệu theo dõi tiến độ tổng thể của hệ thống Quản lý Đặt lịch Khám theo 8 giai đoạn (Phases) đã được quy định trong tài liệu yêu cầu.

---

## 1. Bảng theo dõi tiến độ các Giai đoạn (Project Phases Roadmap)

| Giai đoạn | Tên giai đoạn | Mô tả nội dung | Trạng thái | Hoàn thành |
|---|---|---|---|---|
| **PHASE 1** | **Analysis & Design** | Đọc tài liệu, phân tích 28 Use Case, BR01–BR12, thiết kế kiến trúc, database ERD, UI/UX guidelines, ma trận truy vết | **COMPLETED** | 100% |
| **PHASE 2** | **Foundation & Project Setup** | Khởi tạo Solution .NET, cấu hình DI, EF Core DbContext, kết nối SQL Server (SQLEXPRESS), Entities, Migrations, Seed Data | **COMPLETED** | 100% |
| **PHASE 3** | **Backend Implementation** | Core Services & RESTful API cho UC01–UC28; migration, phân quyền, transaction, outbox và kiểm thử backend | **COMPLETED** | 100% backend; đã kiểm chứng trên SQL Server ngày 17/10/2026 |
| **PHASE 4** | **UI/UX Design System** | CSS/C# tokens, component dùng lại, layout, đăng nhập, dashboard, form hồ sơ, responsive và trạng thái UI nối API thật | **COMPLETED** | 100% phạm vi design system; xem `phase4-verification.md` |
| **PHASE 5** | **Web Implementation** | Patient Web, Staff Portal, Doctor Portal: tài khoản, đặt/đổi/hủy/tiếp nhận và tra cứu qua API thật | **COMPLETED** | 100% phạm vi Web; xem `phase5-verification.md` |
| **PHASE 6** | **Desktop Implementation** | Windows Forms Admin: tài khoản/phân quyền, bác sĩ, chuyên khoa, lịch làm việc/khung giờ, nhật ký và cá nhân qua API thật | **COMPLETED** | 100% phạm vi Desktop; xem `phase6-verification.md` |
| **PHASE 7** | **Integration Testing** | Chuỗi Web ↔ Desktop ↔ API ↔ SQL Server, cạnh tranh từ browser/API, 18 kịch bản bắt buộc, rollback/outbox và hồi quy | **COMPLETED** | 100% phạm vi tích hợp; xem `phase7-verification.md` |
| **PHASE 8** | **UI Polish, Demo Guide & Bàn giao** | Rà soát UI, hồi quy nghiệp vụ, công cụ chạy demo và tài liệu bàn giao | **COMPLETED** | 100% phạm vi đồ án/demo; xem `phase8-verification.md` |

---

## 2. Chi tiết kết quả Phase 1 (Analysis & Design)

- [x] **Khảo sát môi trường và tài liệu đầu vào:**
  - Đã trích xuất và phân tích toàn diện 2 tài liệu: `S6_Nhom01_Lan2.docx` và `De tai mon Ngon ngu lap trinh CSharp - 2026.pdf`.
  - Môi trường phát triển đã sẵn sàng:
    - .NET SDK: `10.0.400` & `9.0.102` (Hỗ trợ đầy đủ .NET 9 LTS/STS và WindowsDesktop cho WinForms).
    - Database Server: `SQL Server (SQLEXPRESS)` đang chạy (`Running`).
- [x] **Tài liệu phân tích nghiệp vụ & yêu cầu:**
  - `docs/requirements-review.md`: Tổng hợp 4 tác nhân, 28 Use Case, 12 Business Rules, phạm vi, các giả định và giải đáp thắc mắc.
- [x] **Tài liệu thiết kế kiến trúc hệ thống:**
  - `docs/architecture.md`: Phân chia 4 tầng Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Presentation`), luồng dữ liệu Web/Desktop dùng chung API/SQL Server, xác thực JWT & Role-based.
- [x] **Tài liệu thiết kế cơ sở dữ liệu:**
  - `docs/database-design.md`: ERD chi tiết, đặc tả 12 bảng chuẩn hóa 3NF, Indexes tối ưu, cơ chế Optimistic Concurrency (`RowVersion`), kế hoạch Seed Data tài khoản mẫu.
- [x] **Tài liệu thiết kế UI/UX & Design System:**
  - `docs/ui-ux-guidelines.md`: Bảng màu Modern Healthcare Teal, typography, đặc tả màn hình cho 4 phân hệ (Patient Web, Staff Portal, Doctor Portal, Desktop Admin WinForms) và 6 trạng thái UI bắt buộc.
- [x] **Ma trận truy vết yêu cầu (Requirement Traceability Matrix):**
  - `docs/usecase-coverage.md`: Bảng đối chiếu từ UC01-UC28 sang BR01-BR12, API Endpoints, UI Screens và 18 Test Cases bắt buộc.

---

## 3. Chi tiết kết quả Phase 2 (Foundation & Project Setup)

- [x] **Khởi tạo Solution & Projects:**
  - `HospitalAppointmentSystem.sln`
  - `src/Hospital.Domain` (.NET 9)
  - `src/Hospital.Application` (.NET 9)
  - `src/Hospital.Infrastructure` (.NET 9)
  - `src/Hospital.Api` (.NET 9)
  - `src/Hospital.Web` (.NET 9)
  - `src/Hospital.Desktop` (.NET 9 Windows Forms)
  - `tests/Hospital.UnitTests` (.NET 9 xUnit)
- [x] **Thiết kế thực thể Domain:**
  - 5 Enums: `UserRole`, `Gender`, `TimeSlotStatus`, `AppointmentStatus`, `NotificationType`.
  - 12 Entities: `Role`, `User`, `Patient`, `Specialty`, `Doctor`, `DoctorSpecialty`, `WorkingSchedule`, `TimeSlot`, `Appointment`, `AppointmentStatusHistory`, `Notification`, `AuditLog`.
- [x] **Cấu hình EF Core & SQL Server (SQLEXPRESS):**
  - `HospitalDbContext` thiết lập Fluent API, Unique Constraints, Foreign Keys, Index tối ưu, và RowVersion cho Concurrency Control.
  - Chuỗi kết nối: `Server=.\\SQLEXPRESS;Database=HospitalDb;Trusted_Connection=True;TrustServerCertificate=True;`.
  - Migration ban đầu `InitialCreate` đã được tạo và áp dụng thành công lên cơ sở dữ liệu `HospitalDb`.
- [x] **Cơ chế Bảo mật & Xác thực:**
  - `IPasswordHasher` & `PasswordHasher` sử dụng thuật toán BCrypt.
  - `IJwtTokenGenerator` & `JwtTokenGenerator` hỗ trợ tạo token kèm claims vai trò và id hồ sơ.
- [x] **Dữ liệu mẫu (Data Seeder):**
  - Đã nạp thành công: 4 vai trò, 6 tài khoản mẫu (`admin`, `staff1`, `doctor1`, `doctor2`, `patient1`, `patient2`), 5 chuyên khoa, 2 bác sĩ, lịch làm việc và khung giờ 7 ngày tới, lịch hẹn mẫu và thông báo.
- [x] **Kiểm thử tự động & API Smoke Test:**
  - Toàn bộ 3 Unit Tests trong `Hospital.UnitTests` vượt qua (100% Passed).
  - Web API khởi chạy và phản hồi thành công endpoint `/` (Status: Running).

## 4. Kết quả Phase 3, cập nhật ngày 17/10/2026

- [x] Hoàn thiện tài khoản, validation, mã bệnh nhân không dựa trên Count, thời hạn JWT đồng nhất với token.
- [x] Kiểm tra tài khoản/quyền/TokenVersion mỗi request; khóa tài khoản, đổi vai trò hoặc mật khẩu vô hiệu token cũ; giới hạn đăng nhập/đăng ký.
- [x] Danh mục có phân trang; tách họ tên bác sĩ khỏi tài khoản; quyền đọc lịch làm việc đúng phạm vi.
- [x] UC13–UC20: đặt/đổi/hủy, kiểm tra slot và chồng lấn, phạm vi Patient/Doctor/Staff, tiếp nhận và NoShow, RowVersion, lịch sử và audit.
- [x] Transaction Serializable, chống gửi lặp bằng RequestKey; unique filtered index cho lịch hẹn còn chiếm slot; giữ slot/lịch sử khi đổi ca trực.
- [x] UC21: đọc/đánh dấu thông báo, outbox có retry, reminder trước 1 giờ và hủy lời nhắc cũ sau thay đổi lịch.
- [x] UC22–UC28: quản trị tài khoản/vai trò/bác sĩ/chuyên khoa; bảo vệ Admin cuối; chặn thay đổi ảnh hưởng lịch Confirmed; không xóa dữ liệu lịch sử.
- [x] Migration Phase3Backend, design-time factory, script nâng cấp, tài liệu API và ma trận truy vết.
- [x] Kiểm thử unit, database quan hệ và HTTP API; xem số liệu xác nhận cuối trong `phase3-verification.md`.
- [x] Sao lưu và kiểm tra bản sao lưu trước khi nâng cấp `HospitalDb`; áp dụng migration `Phase3Backend` trên SQLEXPRESS, giữ 6 tài khoản và 2 lịch hẹn hiện có.
- [x] Chạy đủ 4 test cạnh tranh trực tiếp trên SQL Server: tranh slot, gửi lặp RequestKey, lịch bệnh nhân chồng lấn và bảo vệ Admin cuối.
- [x] Bổ sung 3 test SQL Server: luồng HTTP Admin → Patient → Doctor → Staff, rowversion thật và database không truy cập được rồi phục hồi.
- [x] Sửa middleware nhận diện lỗi SQL bên trong `RetryLimitExceededException`, trả 503 `DATABASE_UNAVAILABLE` kèm trace thay vì 500 sau khi hết lượt retry.
- [x] Build solution: 0 warning, 0 error. Tổng cộng 65 test passed, 0 failed, 0 skipped khi cấu hình `HOSPITAL_TEST_SQLSERVER`.
- [x] Chạy API bình thường với `HospitalDb`: root, readiness, Swagger và đăng nhập/đọc dữ liệu bằng cả 4 vai trò đều thành công.

Môi trường xác nhận: Windows, SDK .NET 10.0.400, runtime .NET/ASP.NET Core 9.0.1, SQL Server 2022 Express 16.0.1000.6 (`.\SQLEXPRESS`). Giữ target net9.0; lần này không cần roll-forward. Chi tiết bằng chứng và lệnh chạy lại ở `phase3-verification.md`.

Ghi nhận ngày 16/10/2026 về việc thiếu SQL Server/runtime 9 thuộc môi trường của lượt trước. Phần kiểm chứng SQL Server được hoàn tất ở lượt ngày 17/10/2026 trên máy hiện tại.

Mức hoàn thành này chỉ dành cho backend. Chưa đánh dấu 28 Use Case hoàn tất xuyên suốt sản phẩm: giao diện vẫn thuộc Phase 5–6; tích hợp Web/Desktop/SQL Server thuộc Phase 7.

## 5. Kết quả Phase 4, cập nhật ngày 17/10/2026

- [x] Một nguồn tokens JSON sinh CSS và C#, có lệnh kiểm tra đồng bộ. Font Segoe UI/system, bảng màu teal/slate, primary đậm để đảm bảo tương phản chữ.
- [x] Component Web: button, input, validation, alert, badge trạng thái, bảng, phân trang, card, empty state, spinner/skeleton, dialog xác nhận và stepper mẫu.
- [x] Layout công khai và portal theo vai trò, menu mobile, skip link, focus bàn phím, reduced motion. Gallery `/Design` giới hạn Development.
- [x] Đăng nhập/đăng xuất thật; dashboard 4 vai trò; tra cứu bác sĩ/chuyên khoa/lịch hẹn, chi tiết và lịch tuần bác sĩ; thông báo và form sửa hồ sơ qua API.
- [x] Client HTTP dùng chung; session hết hạn/thu hồi, phân quyền, CSRF, lỗi kết nối/trace, giữ form khi validation thất bại.
- [x] WinForms: theme, button/field/banner/grid tái sử dụng; đăng nhập Admin; danh sách tài khoản/bác sĩ/chuyên khoa, tìm kiếm, phân trang, loading/empty/error.
- [x] Build 0 warning/error; 71 .NET tests đạt, gồm 6 test Web mới.
- [x] 8 kiểm thử Chromium đạt trên dữ liệu SQL Server riêng, 320/360/430/768/1440px, keyboard/menu/dialog, lưu hồ sơ và axe trên các trang đã triển khai.
- [x] Smoke WinForms chạy message loop thật với cửa sổ ẩn: validation, đăng nhập API, 6 tài khoản/2 bác sĩ/5 chuyên khoa, tìm kiếm rỗng, render tại kích thước chuẩn và tối thiểu.
- [x] Hướng dẫn chạy, cấu trúc component, phạm vi còn lại và bằng chứng kiểm thử được ghi trong `phase4-design-system.md`, `phase4-verification.md`, README.

Phase 5–6 đã có nền tảng dùng lại, chưa hoàn tất: đăng ký/đổi mật khẩu, toàn bộ quy trình đặt/đổi/hủy/tiếp nhận trên Web; CRUD/phân quyền/lịch làm việc trên Desktop. Phase 7 vẫn cần kiểm thử xuyên suốt đầy đủ. Kiểm tra axe và render ẩn không thay thế kiểm tra screen reader, DPI đa màn hình và thao tác GUI thủ công.


## 6. Kết quả Phase 5, cập nhật ngày 18/10/2026

- [x] Đăng ký Patient, đổi mật khẩu và đăng nhập lại; validation, CSRF, cookie và quyền backend.
- [x] Quy trình chọn chuyên khoa/bác sĩ/ngày/giờ, kiểm tra, xác nhận, phiếu hẹn và in; dữ liệu API thật.
- [x] Staff tìm hồ sơ hiện có và đặt hộ; GET patient detail chỉ cho Staff và hồ sơ hoạt động.
- [x] Đổi/hủy theo hạn, Staff bắt buộc lý do; giữ RowVersion ban đầu, hiển thị lỗi xung đột và thao tác tải lại rõ ràng.
- [x] Tiếp nhận có xác nhận đối chiếu; ghi nhận Không đến sau khi hết giờ. Doctor chỉ xem dữ liệu thuộc mình.
- [x] Lọc chuyên khoa, ngày, trạng thái, từ khóa, bác sĩ, thứ tự và phân trang; liên kết lịch sử/sắp tới/hôm nay.
- [x] Bản xác nhận được Data Protection bảo vệ và gắn người dùng; RequestKey cố định để thử lại sau mất phản hồi không tạo lịch trùng.
- [x] Build sạch; 79 .NET tests đạt (8 SQL Server), 11 browser tests đạt; responsive 320–1440px và axe trên các trang kiểm tra.
- [x] Cập nhật hướng dẫn sử dụng, API, ma trận UC và bằng chứng trong `phase5-verification.md`.

Ghi nhận cuối mục Phase 4 là trạng thái tại 17/10; các hạng mục Web còn thiếu lúc đó đã được hoàn tất ở Phase 5. Phase 6 tiếp tục CRUD/phân quyền/lịch làm việc Desktop. Phase 7–8 vẫn chưa được đánh dấu hoàn thành.

## 7. Kết quả Phase 6, cập nhật ngày 18/10/2026

- [x] Tổng quan dữ liệu thật, điều hướng 6 mục, tìm kiếm/phân trang, bộ lọc ngày/bác sĩ và đồng hồ UTC+7.
- [x] Cấp/sửa/khóa/mở khóa tài khoản, đổi vai trò, liên kết hồ sơ bác sĩ; bảo vệ Admin cuối và giữ vai trò Patient.
- [x] Thêm/sửa/ngừng hoạt động bác sĩ, chuyên khoa; chọn nhiều chuyên khoa và chuyên khoa chính; đồng bộ tên bác sĩ/tài khoản.
- [x] Tạo/sửa ca trực, sinh khung 30 phút, giữ lịch sử khung cũ; đóng khung với lý do và bảo vệ lịch hẹn bị ảnh hưởng.
- [x] Nhật ký chỉ đọc với người thực hiện/dữ liệu trước–sau; hồ sơ và đổi mật khẩu Admin, kết thúc phiên khi quyền/mật khẩu thay đổi.
- [x] Form/validation/xác nhận dùng chung; loading/empty/error/success/disabled; không tự gửi lại thao tác ghi khi chưa rõ kết quả.
- [x] Bổ sung API đọc tài khoản, metadata liên kết/chuyên khoa chính và DTO nhật ký; không thêm migration.
- [x] Build 0 warning/error; 82 .NET tests đạt, gồm 8 SQL Server; smoke WinForms 87 assertions qua API/database riêng.
- [x] Cập nhật README, ma trận UC, tài liệu API và hướng dẫn kiểm chứng trong `phase6-verification.md`.

Ghi nhận cuối mục Phase 5 là trạng thái trước khi triển khai Desktop; phần này hoàn tất phạm vi Phase 6. Phase 7–8 giữ Planned, chưa được nghiệm thu thay bằng smoke hoặc test hồi quy.

## 8. Kết quả Phase 7, cập nhật ngày 18/10/2026

- [x] Tự động hóa chuỗi WinForms tạo chuyên khoa/bác sĩ/tài khoản/ca trực → Patient Web chọn/đặt → Doctor xem → Staff tiếp nhận → Patient/Doctor thấy trạng thái mới → Desktop chặn đóng khung đang sử dụng.
- [x] Hai browser context cùng gửi POST đặt một slot; đúng một thành công, một nhận lỗi; đối chiếu SQL không có bản ghi trùng.
- [x] Gửi lại cùng POST đặt, đổi/hủy, ba thông báo và đánh dấu đã đọc; Desktop thấy slot giải phóng rồi đóng được, Web tải lại không cho chọn slot đã đóng.
- [x] 18 kịch bản bắt buộc chạy riêng qua HTTP API trên SQL Server mới, có assertion dữ liệu sau thành công hoặc thất bại.
- [x] Lỗi SaveChanges, outbox phục hồi và timeout SQL thật sau một UPDATE trong transaction; dữ liệu/RowVersion/lịch sử/audit/outbox rollback đầy đủ.
- [x] Runner tạo database riêng, kiểm tra cổng, chờ readiness, giữ log/ảnh, tuân thủ rate limiter và dọn process/database trong finally.
- [x] Sửa trạng thái nút theo dòng hiện tại trên Desktop; hồi quy kiểm tra chuyển từ hồ sơ bác sĩ đã liên kết sang hồ sơ chưa liên kết, runner giới hạn thời gian để không treo ở modal.
- [x] Build 0 warning/error; 102 .NET tests (28 SQL Server), 12 browser tests và 89 assertions WinForms đạt. Tokens đồng bộ.
- [x] README, ma trận truy vết và `phase7-verification.md` ghi cách chạy lại, bằng chứng và giới hạn.

Mốc Planned ở cuối mục Phase 6 là trạng thái trước lượt này. Phase 7 đã nghiệm thu; Phase 8 vẫn Planned, chưa bao gồm rà soát GUI thủ công đầy đủ hoặc tài liệu bàn giao cuối.


## 9. Kết quả Phase 8, cập nhật ngày 19/10/2026

- [x] Rà soát ảnh 48 màn hình Web ở desktop/mobile và 22 màn hình/form WinForms ở kích thước thường/tối thiểu, DPI 120, trên API/SQL thật.
- [x] Sửa menu mobile/focus/nền inert, màn hình thấp, nút thao tác 44 px, xuống dòng form/tiêu đề, định dạng bảng bệnh nhân và retry giữ query.
- [x] Build 0 warning/error; 102 .NET tests (28 SQL), 19 Playwright tests (18 browser + 1 Desktop gallery), 89 assertions Desktop hồi quy và 40 assertions ảnh/form đạt.
- [x] Khởi động/dừng/status demo tự động, bảo vệ cổng/PID, restart giữ dữ liệu; kiểm chứng trên database riêng và dọn sạch.
- [x] Bàn giao Demo Guide từ đầu trên Windows, SQL Express/Docker, migration/seed, tài khoản, kịch bản đồng bộ và chạy lại sau tắt máy.
- [x] Kiến trúc, database 13 bảng theo metadata thực tế, giải thích kỹ thuật tiếng Việt, README và ma trận UC đồng bộ với code.

Các ghi chú Planned ở phần lịch sử là trạng thái của những lượt trước. Phase 1–8 đã hoàn tất trong phạm vi đồ án/demo cục bộ. Giới hạn được ghi rõ trong [Phase 8](phase8-verification.md): chưa chạy Docker, tắt/bật máy vật lý, thử tải lớn hoặc WinForms đa DPI thủ công; không coi các kiểm tra tự động là nghiệm thu production.
