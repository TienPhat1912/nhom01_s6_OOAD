# Kế hoạch & Tiến độ Dự án — Hospital Appointment System

> Tài liệu theo dõi tiến độ tổng thể của hệ thống Quản lý Đặt lịch Khám theo 8 giai đoạn (Phases) đã được quy định trong tài liệu yêu cầu.

---

## 1. Bảng theo dõi tiến độ các Giai đoạn (Project Phases Roadmap)

| Giai đoạn | Tên giai đoạn | Mô tả nội dung | Trạng thái | Hoàn thành |
|---|---|---|---|---|
| **PHASE 1** | **Analysis & Design** | Đọc tài liệu, phân tích 28 Use Case, BR01–BR12, thiết kế kiến trúc, database ERD, UI/UX guidelines, ma trận truy vết | **COMPLETED** | 100% |
| **PHASE 2** | **Foundation & Project Setup** | Khởi tạo Solution .NET, cấu hình DI, EF Core DbContext, kết nối SQL Server (SQLEXPRESS), Entities, Migrations, Seed Data | **COMPLETED** | 100% |
| **PHASE 3** | **Backend Implementation** | Core Services & RESTful API cho UC01–UC28; migration, phân quyền, transaction, outbox và kiểm thử backend | **ĐÃ TRIỂN KHAI; CHỜ KIỂM CHỨNG SQL SERVER** | API/service đã có; chưa xác nhận hoàn tất trên SQL Server |
| **PHASE 4** | **UI/UX Design System** | Xây dựng Tailwind/CSS Design Tokens, Reusable Components, Shared Layouts, States (Loading, Empty, Error, Success) | Planned | 0% |
| **PHASE 5** | **Web Implementation** | Triển khai ASP.NET Core MVC (Patient Web, Staff Portal, Doctor Portal) kết nối API và database thực tế | Planned | 0% |
| **PHASE 6** | **Desktop Implementation** | Triển khai C# Windows Forms Admin hiện đại (Quản trị hệ thống, Bác sĩ, Chuyên khoa, Lịch làm việc, Phân quyền) kết nối API/DB | Planned | 0% |
| **PHASE 7** | **Integration Testing** | Kiểm thử xuyên suốt Web $\leftrightarrow$ Desktop $\leftrightarrow$ API $\leftrightarrow$ SQL Server, Concurrency test, 18 kịch bản bắt buộc | Planned | 0% |
| **PHASE 8** | **UI Polish, Demo Guide & Bàn giao** | Rà soát trải nghiệm, biên soạn Demo Guide, Architecture Explanation, hoàn thiện sản phẩm | Planned | 0% |

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

## 4. Kết quả Phase 3 ngày 16/10/2026

- [x] Hoàn thiện tài khoản, validation, mã bệnh nhân không dựa trên Count, thời hạn JWT đồng nhất với token.
- [x] Kiểm tra tài khoản/quyền/TokenVersion mỗi request; khóa tài khoản, đổi vai trò hoặc mật khẩu vô hiệu token cũ; giới hạn đăng nhập/đăng ký.
- [x] Danh mục có phân trang; tách họ tên bác sĩ khỏi tài khoản; quyền đọc lịch làm việc đúng phạm vi.
- [x] UC13–UC20: đặt/đổi/hủy, kiểm tra slot và chồng lấn, phạm vi Patient/Doctor/Staff, tiếp nhận và NoShow, RowVersion, lịch sử và audit.
- [x] Transaction Serializable, chống gửi lặp bằng RequestKey; unique filtered index cho lịch hẹn còn chiếm slot; giữ slot/lịch sử khi đổi ca trực.
- [x] UC21: đọc/đánh dấu thông báo, outbox có retry, reminder trước 1 giờ và hủy lời nhắc cũ sau thay đổi lịch.
- [x] UC22–UC28: quản trị tài khoản/vai trò/bác sĩ/chuyên khoa; bảo vệ Admin cuối; chặn thay đổi ảnh hưởng lịch Confirmed; không xóa dữ liệu lịch sử.
- [x] Migration Phase3Backend, design-time factory, script nâng cấp, tài liệu API và ma trận truy vết.
- [x] Kiểm thử unit, database quan hệ và HTTP API; xem số liệu xác nhận cuối trong `phase3-verification.md`.
- [ ] Áp dụng migration, chạy backend và 4 test cạnh tranh trực tiếp trên SQL Server của máy triển khai.

Người dùng chọn cấu hình SQL Server sau; bộ test và hướng dẫn kết nối đã sẵn sàng cho bước này.

Giới hạn môi trường lần này: SDK 10.0.402/runtime 10.0.13, không có runtime 9; giữ target net9.0 và chạy test qua roll-forward. Không tìm thấy service SQL Server; Docker engine không chạy. Các thông tin SDK/SQLEXPRESS tại Phase 1–2 phía trên là lịch sử ghi nhận trước đó, không phải kết quả khảo sát hiện tại.

Mức hoàn thành này chỉ dành cho backend. Chưa đánh dấu 28 Use Case hoàn tất xuyên suốt sản phẩm: giao diện vẫn thuộc Phase 5–6; tích hợp Web/Desktop/SQL Server thuộc Phase 7.
