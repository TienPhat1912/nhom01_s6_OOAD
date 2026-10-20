# UI/UX Guidelines & Design System — Hospital Appointment System

> Tài liệu hướng dẫn thiết kế giao diện người dùng (UI), trải nghiệm người dùng (UX) và Design System chuẩn mực cho toàn bộ hệ sinh thái: **Patient Web**, **Staff Portal**, **Doctor Portal** và **Desktop Admin (WinForms)**.

---

## 1. Triết lý thiết kế & Định vị thẩm mỹ

Hệ thống được định vị theo phong cách **Modern Healthcare SaaS**:
- **Sạch sẽ & Đáng tin cậy (Clean & Trustworthy):** Đem lại cảm giác an tâm, chuyên nghiệp của môi trường y tế tiêu chuẩn quốc tế.
- **Tối ưu thao tác (Task-Focused & High Ergonomics):** Giảm thiểu số lượt nhấp chuột, hiển thị rõ ràng thông tin trọng yếu, phản hồi trạng thái tức thời.
- **Nhất quán đa nền tảng (Omni-channel Consistency):** Sự tương đồng về ngôn ngữ thị giác, thuật ngữ và logic trạng thái giữa nền tảng Web và Desktop.
- **Không sử dụng giao diện mặc định thô sơ (Anti-Default UI):** Tuyệt đối không dùng bảng xám hoặc nút bấm xám WinForms cổ điển; sử dụng components hiện đại với bo góc, đổ bóng đa tầng và typography tinh tế.

---

## 2. Design Tokens & Visual Hierarchy

### 2.1. Bảng màu (Color Palette)

| Token Name | Mã Hex | Tên màu / Ứng dụng |
|---|---|---|
| `--color-primary-900` | `#0f3b38` | Deep Teal Dark (Header, Sidebar background) |
| `--color-primary-700` | `#0d5e56` | Medical Teal Hover |
| `--color-primary-600` | `#0d9488` | **Brand Primary Teal** (Action buttons, Active states) |
| `--color-primary-100` | `#ccfbf1` | Teal Tint (Badge, Highlight background) |
| `--color-primary-50` | `#f0fdfa` | Teal Surface Light (Table row hover, Card tint) |
| `--color-secondary-600` | `#0284c7` | Clinical Cyan/Blue (Secondary CTA, Links) |
| `--color-slate-900` | `#0f172a` | Primary Text (Headings, Heavy data) |
| `--color-slate-700` | `#334155` | Body Text |
| `--color-slate-500` | `#64748b` | Muted/Secondary Text, Form hints |
| `--color-slate-200` | `#e2e8f0` | Borders, Dividers, Inactive state |
| `--color-slate-100` | `#f1f5f9` | Card Background, Input Background |
| `--color-slate-50` | `#f8fafc` | General App Background |
| `--color-surface` | `#ffffff` | Pure White Surfaces & Cards |
| `--color-success-600` | `#16a34a` | Trạng thái: Confirmed (Đã xác nhận), Tiếp nhận thành công |
| `--color-warning-600` | `#d97706` | Trạng thái: Sắp diễn ra, Cảnh báo khung giờ sát giờ |
| `--color-danger-600` | `#dc2626` | Trạng thái: Cancelled (Đã hủy), Lỗi thao tác, Nút Hủy |
| `--color-info-600` | `#2563eb` | Trạng thái: CheckedIn (Đã tiếp nhận), Thông báo hệ thống |
| `--color-neutral-600` | `#475569` | Trạng thái: NoShow (Không đến) |

### 2.2. Kiểu chữ (Typography)
- **Primary Font Family:** `Inter`, `Be Vietnam Pro`, `system-ui, -apple-system, sans-serif`
- **Chỉ số kích thước & Line-height:**
  - `Display / H1`: 32px / Bold (700) / Tracking: -0.02em
  - `H2 (Section titles)`: 24px / SemiBold (600)
  - `H3 (Card headers)`: 18px / SemiBold (600)
  - `Body Regular`: 14px / Regular (400) / Line-height: 1.5
  - `Body Medium`: 14px / Medium (500)
  - `Caption / Meta`: 12px / Regular (400) hoặc Medium (500)

### 2.3. Khối hình & Đổ bóng (Border Radius & Elevations)
- `Radius Small (sm)`: 6px (Badges, tags, form controls nhỏ)
- `Radius Medium (md)`: 10px (Input boxes, buttons, notification toasts)
- `Radius Large (lg)`: 16px (Cards, modal dialogs, dashboard widgets)
- `Radius Full`: 9999px (Avatar, status pills, round action buttons)
- `Shadow Soft`: `0 1px 3px 0 rgba(0, 0, 0, 0.05), 0 1px 2px -1px rgba(0, 0, 0, 0.05)`
- `Shadow Card`: `0 4px 6px -1px rgba(0, 0, 0, 0.07), 0 2px 4px -2px rgba(0, 0, 0, 0.05)`
- `Shadow Floating`: `0 10px 15px -3px rgba(0, 0, 0, 0.08), 0 4px 6px -4px rgba(0, 0, 0, 0.05)`

---

## 3. Đặc tả chi tiết các phân hệ giao diện

### 3.1. Phân hệ Patient Web Application (Bệnh nhân)

#### A. Trang chủ (Landing Page)
1. **Header Navigation:**
   - Logo thương hiệu bệnh viện + Tên hệ thống.
   - Menu liên kết nhanh: Trang chủ, Chuyên khoa, Bác sĩ, Hướng dẫn đặt lịch.
   - Góc phải: Nút "Tra cứu lịch hẹn", Nút "Đăng nhập" / "Đăng ký" (khi chưa login) hoặc Dropdown Tên bệnh nhân + Avatar + Thông báo (khi đã login).
2. **Hero Section:**
   - Tiêu đề ấn tượng: "Đặt lịch khám chuyên gia nhanh chóng & chuẩn xác".
   - Thanh tìm kiếm tương tác (Smart Search Bar): Lọc theo Tên Bác sĩ hoặc Chuyên khoa với gợi ý tức thì.
   - Nút Call-To-Action (CTA) nổi bật: "Đặt lịch ngay".
3. **Danh mục Chuyên khoa nổi bật (Featured Specialties):**
   - Lưới thẻ (Cards) trực quan với icon sinh động (Nội khoa, Nhi khoa, Da liễu, Tai Mũi Họng, v.v.).
   - Hiển thị số lượng bác sĩ trực thuộc mỗi khoa.
4. **Đội ngũ Bác sĩ tiêu biểu:**
   - Thẻ bác sĩ (Doctor Cards): Ảnh chân dung sắc nét, Học vị/Chức danh, Chuyên khoa chính, Số năm kinh nghiệm, Nút "Xem lịch trống".
5. **Quy trình 4 bước đặt lịch minh họa:**
   - Chọn chuyên khoa & bác sĩ $\rightarrow$ Chọn khung giờ $\rightarrow$ Xác nhận thông tin $\rightarrow$ Nhận mã hẹn.

#### B. Quy trình Đặt lịch (7-Step Booking Flow)
Quy trình được thể hiện qua **Progress Stepper** rõ ràng:
- **Bước 1: Chọn Chuyên khoa:** Danh sách chuyên khoa có ô lọc và mô tả chi tiết.
- **Bước 2: Chọn Bác sĩ:** Lọc theo chuyên khoa đã chọn, hiển thị hồ sơ vắn tắt của bác sĩ.
- **Bước 3: Chọn Ngày khám:** Bộ lịch trực quan (Interactive Date Picker), làm mờ ngày quá khứ và các ngày bác sĩ không có lịch làm việc.
- **Bước 4: Chọn Khung giờ (TimeSlot Selection):**
  - Hiển thị lưới các khung giờ sáng / chiều (30 phút/slot).
  - Trạng thái rõ ràng: *Còn trống (Open - xanh lục)*, *Đã kín (Occupied - xám mờ có gạch)*, *Đã đóng (Closed)*.
- **Bước 5: Kiểm tra thông tin:** Tóm tắt Bác sĩ, Chuyên khoa, Thời gian, Thông tin cá nhân bệnh nhân, Nhập ghi chú triệu chứng/lý do khám.
- **Bước 6: Xác nhận đặt lịch:** Nút bấm có hiệu ứng loading, gửi API transaction nguyên tử.
- **Bước 7: Kết quả đặt lịch (Booking Success Screen):**
  - Thẻ thông tin vé khám chứa **Mã lịch hẹn (Appointment Code)** định dạng nổi bật (ví dụ: `APT-20261016-0012`).
  - Hướng dẫn thủ tục tiếp đón khi đến khám.
  - Nút in/tải phiếu hoặc "Xem danh sách lịch hẹn của tôi".

#### C. Bảng điều khiển Bệnh nhân (Patient Dashboard & My Appointments)
- **Tab 1: Lịch hẹn sắp tới:** Danh sách lịch `Confirmed` với bộ đếm ngược thời gian, thẻ chi tiết địa điểm, phòng khám, kèm 2 hành động:
  - Nút **"Đổi lịch"** (Hiển thị nếu còn $\ge 2$ giờ trước giờ khám).
  - Nút **"Hủy lịch"** (Hiển thị nếu còn $\ge 2$ giờ trước giờ khám, yêu cầu xác nhận modal).
- **Tab 2: Lịch sử khám:** Danh sách lịch đã hoàn tất (`CheckedIn`), `Cancelled`, hoặc `NoShow`.
- **Trung tâm Thông báo:** Danh sách thông báo (Xác nhận đặt, Đổi lịch, Hủy lịch, Nhắc hẹn 1h trước khám) kèm đánh dấu đã đọc.
- **Hồ sơ cá nhân:** Cập nhật SĐT, địa chỉ, ngày sinh, đổi mật khẩu.

---

### 3.2. Phân hệ Staff Portal (Nhân viên tiếp nhận)

- **Layout:** Thiết kế Compact Toolbar để thao tác nhanh tại quầy tiếp đón.
- **Thanh tìm kiếm tiếp nhận nhanh (Quick Search):**
  - Nhập Mã lịch hẹn, Số điện thoại hoặc Họ tên bệnh nhân $\rightarrow$ Ra ngay kết quả trong 200ms.
- **Màn hình Tiếp nhận bệnh nhân (Check-in Desk):**
  - Danh sách lịch hẹn trong ngày hôm nay.
  - Nút thao tác nhanh 1-Click: **"Tiếp nhận"** $\rightarrow$ Hệ thống chuyển trạng thái sang `CheckedIn`, ghi nhận thời gian và nhân viên xử lý.
- **Ghi nhận vắng mặt (No-Show Handling):**
  - Bộ lọc các lịch hẹn đã qua giờ kết thúc khung giờ mà bệnh nhân chưa đến tiếp nhận.
  - Nút **"Ghi nhận không đến"** (Chỉ kích hoạt khi `EndTime < Now`).
- **Đặt lịch hỗ trợ (Assisted Booking):**
  - Form tìm kiếm hồ sơ bệnh nhân cũ hoặc tạo nhanh hồ sơ bệnh nhân mới $\rightarrow$ Thực hiện các bước đặt lịch thay bệnh nhân.
- **Xử lý Đổi/Hủy lịch hỗ trợ:**
  - Được phép đổi/hủy lịch cho bệnh nhân trước thời điểm bắt đầu khám (linh hoạt hơn quyền tự thao tác của bệnh nhân).
  - Bắt buộc điền **"Lý do thay đổi"** để lưu vào `AuditLog` và `AppointmentStatusHistory`.

---

### 3.3. Phân hệ Doctor Portal (Bác sĩ)

- **Triết lý:** Tối giản, tập trung lịch trình làm việc, không có tính năng rườm rà.
- **Tổng quan hôm nay (Doctor's Daily Agenda):**
  - Thẻ thống kê: Tổng số ca khám hôm nay, Số ca đã tiếp nhận, Số ca đang chờ.
  - Timeline theo từng khung giờ trong ngày: Hiển thị tên bệnh nhân, giờ bắt đầu - kết thúc, trạng thái (`Confirmed`, `CheckedIn`, `NoShow`, `Cancelled`).
- **Lịch làm việc của tôi (My Schedule):**
  - Lịch tuần/tháng thể hiện các ca trực đã được phân công bởi Quản trị viên (Chế độ xem Read-only).
- **Hồ sơ Bác sĩ:**
  - Bác sĩ xem và cập nhật tiểu sử giới thiệu, ảnh đại diện, đổi mật khẩu.

---

### 3.4. Phân hệ Desktop Admin (Windows Forms Hiện đại)

Giao diện Windows Forms được thiết kế tùy biến hoàn toàn theo phong cách **Modern Fluent/SaaS Dashboard**, không sử dụng giao diện màu xám cổ điển:
1. **Sidebar Navigation (Thanh điều hướng bên trái):**
   - Logo bệnh viện + Phiên bản hệ thống.
   - Nhóm menu:
     - 📊 *Dashboard Tổng quan*
     - 👥 *Quản lý Tài khoản & Phân quyền*
     - 👨‍⚕️ *Quản lý Bác sĩ*
     - 🏥 *Quản lý Chuyên khoa*
     - 📅 *Lịch làm việc & Khung giờ*
     - 📜 *Nhật ký Hệ thống (Audit Log)*
   - Phần chân sidebar: Thông tin Admin đang đăng nhập + Nút Đăng xuất.
2. **Top Header Bar:**
   - Tiêu đề trang hiện tại.
   - Chỉ báo trạng thái kết nối Database / API (Xanh: Connected, Đỏ: Disconnected).
   - Đồng hồ thời gian thực và nút làm mới dữ liệu.
3. **Data Grid & Management Forms:**
   - **Modern DataGridView:** Header màu xanh teal đậm (`#0f3b38`), chữ trắng, font rõ ràng, xen kẽ dòng chẵn/lẻ (Zebra striping `#f8fafc` và `#ffffff`), hiệu ứng hover nhẹ khi di chuột.
   - **Hộp thoại (Custom Modal Dialogs):** Form thêm/sửa có border radius, validation hiển thị nhãn đỏ ngay dưới từng ô input, nút bấm primary nổi bật.
   - **Xử lý Lịch làm việc & Khung giờ:**
     - Xem lịch trực theo tuần của từng bác sĩ.
     - Nút "Thêm lịch làm việc" $\rightarrow$ Chọn bác sĩ, ngày làm việc, giờ bắt đầu, giờ kết thúc $\rightarrow$ Tự động tính toán và sinh các slot 30 phút.
     - Chức năng **"Đóng khung giờ"**: Có modal hỏi lý do đóng và cảnh báo nếu slot đang có lịch `Confirmed` (Áp dụng triệt để BR09).

---

## 4. Các trạng thái giao diện bắt buộc (UI States)

Mỗi màn hình và tương tác trên cả Web và Desktop đều phải tuân thủ nghiêm ngặt 6 trạng thái cơ bản:

```
┌────────────────────────────────────────────────────────────────────────┐
│                        6 TRẠNG THÁI GIAO DIỆN CHUẨN                    │
├────────────────────────────────────────────────────────────────────────┤
│ 1. Loading State   │ Spinner mượt mà, Skeleton shimmer khi tải dữ liệu │
│ 2. Empty State     │ Minh họa vector/icon, thông điệp hướng dẫn rõ ràng│
│ 3. Error State     │ Thông báo lỗi kèm mã trace, gợi ý cách khắc phục  │
│ 4. Success State   │ Toast notification xanh, xác nhận trực quan       │
│ 5. Disabled State  │ Làm mờ (opacity 0.5), cursor not-allowed, tooltip │
│ 6. Validation State│ Viền đỏ, thông báo lỗi trực tiếp bên dưới input   │
└────────────────────────────────────────────────────────────────────────┘
```

1. **Loading State:**
   - Nút bấm khi nhấn chuyển sang dạng Spinner + Disable chống click liên tiếp.
   - Danh sách bảng hiển thị Skeleton Loader khi đang fetch dữ liệu từ API.
2. **Empty State:**
   - Ví dụ khi chưa có lịch hẹn: Hiển thị icon lịch trống, tiêu đề "Bạn chưa có lịch hẹn nào", kèm nút CTA "Đặt lịch khám ngay".
3. **Error State:**
   - Toast thông báo màu đỏ góc màn hình hoặc Alert banner nổi bật. Không để trang web trắng hoặc crash desktop app khi có lỗi mạng/lỗi API.
4. **Success State:**
   - Modal chúc mừng khi đặt lịch thành công, Toast thông báo xanh khi cập nhật thông tin/đổi trạng thái.
5. **Confirmation Dialogs:**
   - Thao tác quan trọng (Hủy lịch, Khóa tài khoản, Ngừng hoạt động bác sĩ, Đóng khung giờ) **bắt buộc** phải hiển thị Modal xác nhận với câu hỏi rõ ràng và 2 nút: "Hủy bỏ" (Secondary) và "Xác nhận" (Danger / Primary).
