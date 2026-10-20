# Kiến trúc Hệ thống — Hospital Appointment System

---

## 1. Tổng quan kiến trúc

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           PRESENTATION LAYER                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │ Patient Web  │  │ Staff Portal │  │ Doctor Portal│  │ Desktop Admin │  │
│  │  (MVC/Razor) │  │  (MVC/Razor) │  │  (MVC/Razor) │  │  (WinForms)   │  │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └───────┬───────┘  │
│         │                  │                  │                   │          │
│         └──────────────────┴──────────────────┴───────────────────┘          │
│                                    │                                         │
│                             HTTP / REST API                                  │
│                                    ▼                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                             API LAYER                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                     Hospital.Api (ASP.NET Core)                     │    │
│  │  Controllers → DTOs → Validation → Auth → Middleware → Logging     │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                    │                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                          APPLICATION LAYER                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                  Hospital.Application                               │    │
│  │  Services → Use Cases → Validators → Mappers → Interfaces          │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                    │                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                            DOMAIN LAYER                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    Hospital.Domain                                   │    │
│  │  Entities → Enums → Business Rules → Value Objects                  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                    │                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                         INFRASTRUCTURE LAYER                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                  Hospital.Infrastructure                             │    │
│  │  DbContext → Repositories → EF Core → Migrations → Seed Data       │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                    │                                         │
│                             Microsoft SQL Server                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Cấu trúc Solution

```
HospitalAppointmentSystem/
├── src/
│   ├── Hospital.Domain/              # Entities, Enums, Value Objects, Interfaces
│   ├── Hospital.Application/         # Services, DTOs, Validators, Mapping
│   ├── Hospital.Infrastructure/      # EF Core, DbContext, Repos, Migrations
│   ├── Hospital.Api/                 # ASP.NET Core Web API (Backend)
│   ├── Hospital.Web/                 # ASP.NET Core MVC (Patient, Staff, Doctor portals)
│   ├── Hospital.Desktop/             # Windows Forms Admin App
│   └── Hospital.SharedKernel/        # Shared DTOs, Constants, Helpers
├── tests/
│   ├── Hospital.UnitTests/           # xUnit unit tests
│   ├── Hospital.IntegrationTests/    # Integration tests
│   └── Hospital.ApiTests/            # API endpoint tests
├── docs/
│   ├── requirements-review.md
│   ├── architecture.md
│   ├── database-design.md
│   ├── ui-ux-guidelines.md
│   ├── usecase-coverage.md
│   ├── api-documentation.md
│   ├── demo-guide.md
│   └── project-progress.md
├── scripts/
├── docker-compose.yml
├── README.md
└── HospitalAppointmentSystem.sln
```

---

## 3. Chi tiết từng Project

### Hospital.Domain
- **Entities:** User, Role, Patient, Doctor, Specialty, DoctorSpecialty, WorkingSchedule, TimeSlot, Appointment, AppointmentStatusHistory, Notification, AuditLog
- **Enums:** UserRole, AppointmentStatus, TimeSlotStatus, AccountStatus, Gender
- **Interfaces:** IRepository<T>, IUnitOfWork
- **Value Objects:** DateRange, TimeRange

### Hospital.Application
- **Services:** AccountService, AuthService, SpecialtyService, DoctorService, WorkingScheduleService, TimeSlotService, AppointmentService, NotificationService, AuditService
- **DTOs:** Request/Response DTOs cho mỗi use case
- **Validators:** FluentValidation validators
- **Interfaces:** Service interfaces cho DI

### Hospital.Infrastructure
- **Data:** HospitalDbContext, Entity configurations, Migrations
- **Repositories:** GenericRepository, UnitOfWork
- **Seeding:** DataSeeder (demo data)
- **Services:** JwtTokenService, PasswordHashService, DateTimeService

### Hospital.Api
- **Controllers:** AuthController, AccountController, SpecialtyController, DoctorController, WorkingScheduleController, TimeSlotController, AppointmentController, NotificationController
- **Middleware:** ExceptionHandling, RequestLogging, Authentication
- **Filters:** RoleAuthorization, ModelValidation

### Hospital.Web
- **Areas:** Patient, Staff, Doctor (tách biệt 3 portal)
- **Controllers:** Mỗi area có controllers riêng
- **Views:** Razor views với shared layout
- **wwwroot:** CSS, JS, Images

### Hospital.Desktop
- **Forms:** LoginForm, MainForm, DashboardForm
- **UserControls:** Sidebar, DataGrid, FormPanels
- **Services:** ApiClient (gọi Backend API)

### Hospital.SharedKernel
- **Constants:** AppRoles, AppointmentStatuses
- **DTOs:** Shared request/response models
- **Helpers:** DateTimeHelper, StringHelper

---

## 4. Công nghệ sử dụng

| Thành phần | Công nghệ | Phiên bản |
|-----------|-----------|-----------|
| Runtime | .NET | Target 9.0 hiện tại |
| Backend API | ASP.NET Core Web API | 9.0 |
| Web Frontend | ASP.NET Core MVC + Razor Views | 9.0 |
| Desktop | Windows Forms | .NET 9.0 |
| ORM | Entity Framework Core | 9.0 |
| Database | Microsoft SQL Server | 2022 |
| Authentication | JWT Bearer Token | — |
| Validation | FluentValidation | Latest |
| Testing | xUnit + Moq | Latest |
| Logging | Serilog | Latest |
| CSS | Tailwind CSS (Web) | 3.x |

---

## 5. Luồng xử lý chính

### 5.1. Đặt lịch khám (UC13)
```
Patient/Staff → Web/API → AppointmentService
  1. Xác thực người dùng (JWT)
  2. Tra cứu chuyên khoa (UC05)
  3. Tra cứu bác sĩ (UC06)
  4. Xem khung giờ trống (UC08)
  5. Kiểm tra điều kiện đặt (UC14):
     a. Bác sĩ & chuyên khoa còn hoạt động?
     b. Khung giờ thuộc lịch LV, đang mở, chưa bắt đầu?
     c. Khung giờ chưa bị lịch khác chiếm?
     d. BN không có lịch chồng lấn?
  6. Transaction:
     a. Tạo Appointment (status = Confirmed)
     b. Chiếm TimeSlot
     c. Ghi AuditLog
     d. Tạo Notification
  7. Trả kết quả + mã lịch hẹn
```

### 5.2. Đổi lịch (UC17)
```
Patient/Staff → Web/API → AppointmentService
  1. Kiểm tra quyền sở hữu/phạm vi
  2. Kiểm tra trạng thái = Confirmed
  3. Kiểm tra thời hạn (BN ≥ 2h, NV: trước giờ bắt đầu)
  4. Chọn khung giờ mới
  5. UC14 (loại trừ lịch đang đổi)
  6. Transaction:
     a. Cập nhật Appointment → khung giờ mới
     b. Giải phóng khung giờ cũ
     c. Chiếm khung giờ mới
     d. Ghi AuditLog
     e. Cập nhật/tạo Notification
  7. Nếu thất bại → rollback, giữ lịch cũ
```

### 5.3. Web ↔ Desktop dùng chung dữ liệu
```
┌──────────┐     HTTP/REST     ┌──────────┐     EF Core     ┌──────────┐
│   Web    │ ──────────────── │   API    │ ─────────────── │ SQL Server│
│  (MVC)   │                  │ (ASP.NET)│                  │           │
└──────────┘                  └──────────┘                  └──────────┘
                                    ▲                            ▲
┌──────────┐     HTTP/REST          │                            │
│ Desktop  │ ───────────────────────┘                            │
│(WinForms)│     (gọi cùng API)                                 │
└──────────┘                                                     │
                                                     Shared Database
```

---

## 6. Authentication & Authorization

### JWT Flow
1. User gửi credentials → `/api/auth/login`
2. API verify → tạo JWT token (chứa UserId, Role, Claims)
3. Client lưu token → gửi trong Authorization header
4. API middleware kiểm tra token mỗi request
5. Role-based + Resource-based authorization

### Roles & Permissions
| Quyền | Patient | Doctor | Staff | Admin |
|-------|---------|--------|-------|-------|
| Đăng ký TK | ✅ | ❌ | ❌ | ❌ |
| Xem lịch của mình | ✅ | ✅ | ❌ | ❌ |
| Xem tất cả lịch | ❌ | ❌ | ✅ | ❌ |
| Đặt/đổi/hủy lịch | ✅ (của mình) | ❌ | ✅ (hỗ trợ) | ❌ |
| Tiếp nhận BN | ❌ | ❌ | ✅ | ❌ |
| Quản lý lịch LV | ❌ | ❌ (chỉ xem) | ❌ | ✅ |
| Quản lý TK/BS/CK | ❌ | ❌ | ❌ | ✅ |

---

## 7. Error Handling & Logging

### Error Response Format
```json
{
  "success": false,
  "message": "Khung giờ đã được đặt bởi người khác",
  "errorCode": "TIMESLOT_OCCUPIED",
  "errors": [],
  "timestamp": "2026-10-16T12:00:00Z"
}
```

### Logging Strategy
- **Structured logging** với Serilog
- **Request logging** middleware
- **Audit logging** cho thao tác quan trọng (đặt, đổi, hủy, phân quyền, ...)
- **Error logging** với stack trace (chỉ trong development)
