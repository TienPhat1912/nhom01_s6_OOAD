# Requirements Review — Hệ thống Quản lý Đặt lịch Khám

> Tài liệu này tổng hợp kết quả phân tích từ hai nguồn đầu vào:
> - `S6_Nhom01_Lan2.docx` — Phân tích nghiệp vụ & Use Case
> - `De tai mon Ngon ngu lap trinh CSharp - 2026.pdf` — Yêu cầu kỹ thuật & tiêu chí đánh giá

---

## 1. Tác nhân (Actors)

| Tác nhân | Vai trò | Giao diện |
|----------|---------|-----------|
| **Bệnh nhân** | Tự đăng ký, đăng nhập; tra cứu chuyên khoa, bác sĩ, khung giờ; đặt, xem, đổi, hủy lịch; xem thông báo | Patient Web |
| **Bác sĩ** | Đăng nhập; xem lịch làm việc & lịch hẹn thuộc mình; quản lý thông tin cá nhân | Doctor Portal (Web) |
| **Nhân viên** | Đăng nhập; tra cứu; hỗ trợ đặt/đổi/hủy lịch; tiếp nhận bệnh nhân; ghi nhận không đến | Staff Portal (Web) |
| **Quản trị viên** | Đăng nhập; quản lý tài khoản, bác sĩ, chuyên khoa, lịch làm việc, phân quyền | Desktop Admin (WinForms) |

> **Ghi chú:** "Người dùng" trong tài liệu là actor khái quát cho 4 vai trò khi biểu diễn UC chung (đăng nhập, cập nhật thông tin, đổi mật khẩu).

---

## 2. Danh sách 28 Use Case

### 2.1. Nhóm Tài khoản
| Mã | Tên Use Case | Tác nhân | BR liên quan |
|----|-------------|----------|-------------|
| UC01 | Đăng ký tài khoản | Bệnh nhân | BR01, BR11, BR12 |
| UC02 | Đăng nhập | Tất cả | BR01, BR12 |
| UC03 | Cập nhật thông tin cá nhân | Tất cả | BR01, BR11, BR12 |
| UC04 | Đổi mật khẩu | Tất cả | BR01, BR12 |

### 2.2. Nhóm Tra cứu
| Mã | Tên Use Case | Tác nhân | BR liên quan |
|----|-------------|----------|-------------|
| UC05 | Tra cứu chuyên khoa | Bệnh nhân, Nhân viên | BR01, BR03, BR11, BR12 |
| UC06 | Tra cứu bác sĩ | Bệnh nhân, Nhân viên | BR01, BR03, BR11 |
| UC07 | Xem thông tin bác sĩ | Bệnh nhân, Nhân viên | BR01, BR11, BR12 |
| UC08 | Xem khung giờ trống | Bệnh nhân, Nhân viên | BR02, BR03, BR07, BR12 |

### 2.3. Nhóm Lịch làm việc
| Mã | Tên Use Case | Tác nhân | BR liên quan |
|----|-------------|----------|-------------|
| UC09 | Xem lịch làm việc | Bác sĩ, Quản trị viên | BR01, BR02, BR12 |
| UC10 | Tạo lịch làm việc | Quản trị viên | BR02, BR03, BR11, BR12 |
| UC11 | Điều chỉnh lịch làm việc | Quản trị viên | BR02, BR03, BR09, BR11, BR12 |
| UC12 | Đóng khung giờ | Quản trị viên | BR02, BR03, BR09, BR11, BR12 |

### 2.4. Nhóm Đặt lịch khám
| Mã | Tên Use Case | Tác nhân | BR liên quan |
|----|-------------|----------|-------------|
| UC13 | Đặt lịch khám | Bệnh nhân, Nhân viên | BR01, BR03-05, BR07, BR10-12 |
| UC14 | Kiểm tra điều kiện đặt | (Dùng chung bởi UC13, UC17) | BR03, BR04, BR07, BR12 |

### 2.5. Nhóm Lịch hẹn
| Mã | Tên Use Case | Tác nhân | BR liên quan |
|----|-------------|----------|-------------|
| UC15 | Xem lịch hẹn | Bệnh nhân, Bác sĩ, Nhân viên | BR01, BR05, BR06, BR12 |
| UC16 | Tra cứu lịch hẹn | Nhân viên | BR01, BR05, BR06, BR08 |
| UC17 | Đổi lịch hẹn | Bệnh nhân, Nhân viên | BR01, BR03-04, BR06-07, BR10-12 |
| UC18 | Hủy lịch hẹn | Bệnh nhân, Nhân viên | BR01, BR05-07, BR10-12 |

### 2.6. Nhóm Tiếp nhận
| Mã | Tên Use Case | Tác nhân | BR liên quan |
|----|-------------|----------|-------------|
| UC19 | Tiếp nhận bệnh nhân | Nhân viên | BR01, BR05, BR08, BR11, BR12 |
| UC20 | Ghi nhận bệnh nhân không đến | Nhân viên | BR01, BR05, BR08, BR11, BR12 |

### 2.7. Nhóm Thông báo
| Mã | Tên Use Case | Tác nhân | BR liên quan |
|----|-------------|----------|-------------|
| UC21 | Xem thông báo | Bệnh nhân | BR01, BR05, BR10, BR12 |

### 2.8. Nhóm Quản trị
| Mã | Tên Use Case | Tác nhân | BR liên quan |
|----|-------------|----------|-------------|
| UC22 | Cấp tài khoản | Quản trị viên | BR01, BR11, BR12 |
| UC23 | Cập nhật tài khoản | Quản trị viên | BR01, BR11, BR12 |
| UC24 | Phân quyền tài khoản | Quản trị viên | BR01, BR11, BR12 |
| UC25 | Thêm bác sĩ | Quản trị viên | BR01, BR03, BR11, BR12 |
| UC26 | Cập nhật bác sĩ | Quản trị viên | BR01, BR09, BR11, BR12 |
| UC27 | Thêm chuyên khoa | Quản trị viên | BR01, BR11, BR12 |
| UC28 | Cập nhật chuyên khoa | Quản trị viên | BR01, BR09, BR11, BR12 |

---

## 3. Business Rules (BR01–BR12) — Tóm tắt

| Mã | Tên | Nội dung chính |
|----|-----|----------------|
| BR01 | Phân quyền & tài khoản | BN tự đăng ký; BS/NV/Admin do admin cấp. Mỗi thao tác kiểm tra quyền. BN chỉ quản lý lịch mình, BS chỉ xem lịch thuộc mình. |
| BR02 | Quản lý lịch làm việc | Admin tạo/sửa lịch & đóng khung giờ. BS chỉ xem. NV xem khả dụng qua tra cứu. |
| BR03 | Khung giờ khả dụng | Mỗi khung giờ chỉ phục vụ 1 lịch hẹn có hiệu lực. Phải còn trống, chưa bắt đầu, đang mở. Không chồng lấn lịch làm việc cùng BS. |
| BR04 | Chống trùng lịch BN | BN không được có 2 lịch có hiệu lực giao nhau. Khi đổi, loại trừ lịch đang đổi. Tiếp giáp ≠ chồng lấn. |
| BR05 | Trạng thái lịch hẹn | 4 trạng thái: Đã xác nhận, Đã hủy, Đã tiếp nhận, Không đến. Đặt hợp lệ → xác nhận ngay. |
| BR06 | Điều kiện đổi/hủy | BN: ≥ 2h trước giờ khám, chỉ lịch Đã xác nhận. NV: trước giờ bắt đầu, phải ghi lý do. |
| BR07 | Tính nguyên tử | Đặt/đổi/hủy dùng transaction. Đổi thất bại giữ lịch cũ. Gửi lặp không tạo trùng. |
| BR08 | Tiếp nhận & không đến | Tiếp nhận: chỉ lịch Đã xác nhận, đối chiếu BN. Không đến: chỉ sau khi khung giờ kết thúc & chưa tiếp nhận. |
| BR09 | Ảnh hưởng khi thay đổi lịch LV | Không điều chỉnh/đóng làm mất hiệu lực lịch Đã xác nhận. NV xử lý đổi/hủy trước, admin mới hoàn tất. |
| BR10 | Thông báo & nhắc lịch | Tạo thông báo khi đặt/đổi/hủy thành công. Nhắc trước 1h. Lịch đặt trong 1h dùng thông báo xác nhận. Lỗi thông báo ≠ mất lịch. |
| BR11 | Dữ liệu nền & truy vết | Mã unique, audit log. Ngừng hoạt động không xóa liên kết lịch sử. Không ngừng admin cuối cùng. |
| BR12 | An toàn thông tin | Hash mật khẩu. Kiểm tra phiên, quyền, trạng thái TK. Lỗi lưu không tạo dữ liệu dở dang. |

---

## 4. Phạm vi & Giới hạn

### Trong phạm vi
- Đăng ký, đăng nhập, quản lý tài khoản
- Tra cứu chuyên khoa, bác sĩ, khung giờ
- Quản lý lịch làm việc
- Đặt, đổi, hủy lịch khám
- Tiếp nhận bệnh nhân
- Thông báo & nhắc lịch
- Quản lý dữ liệu hệ thống & phân quyền

### Ngoài phạm vi (KHÔNG triển khai)
- Chẩn đoán, kê đơn, xét nghiệm
- Quản lý bệnh án, điều trị
- Thanh toán
- Gửi thông báo qua kênh bên ngoài (email, SMS)
- Microservices, message broker

---

## 5. Giả định hợp lý

| # | Giả định | Lý do |
|---|----------|-------|
| A1 | Thông tin bệnh nhân khi đăng ký bao gồm: họ tên, ngày sinh, giới tính, số điện thoại, email, địa chỉ, username, password | Tài liệu UC01 yêu cầu "thông tin cá nhân, liên hệ và đăng nhập" nhưng không liệt kê cụ thể |
| A2 | Thông tin bác sĩ bao gồm: mã BS, họ tên, chuyên khoa, ảnh đại diện, mô tả giới thiệu, trạng thái | UC07 và UC25 đề cập "thông tin công khai" và "thông tin giới thiệu" |
| A3 | Khung giờ có thời lượng cố định (30 phút), có thể cấu hình | Tài liệu nói "khung giờ" nhưng không quy định thời lượng cụ thể |
| A4 | Nhắc lịch bằng cách tạo Notification trong DB, không gửi email/SMS | Phạm vi đề tài không bao gồm tích hợp bên ngoài |
| A5 | Web session dùng JWT token, phiên hết hạn yêu cầu đăng nhập lại | BR12 yêu cầu kiểm tra phiên nhưng không chỉ định công nghệ |
| A6 | Quản trị viên sử dụng Desktop app, có thể truy cập Web nếu cần | Tài liệu phân rõ Desktop cho admin |
| A7 | Một bác sĩ thuộc 1 chuyên khoa chính | UC25 nói "gắn với chuyên khoa" (số ít) — sẽ thiết kế hỗ trợ nhiều nếu cần |

---

## 6. Điểm chưa rõ hoặc cần quyết định

| # | Vấn đề | Quyết định |
|---|--------|-----------|
| Q1 | Bác sĩ có thể thuộc nhiều chuyên khoa không? | **Quyết định:** Hỗ trợ nhiều chuyên khoa (bảng DoctorSpecialty). Tài liệu không cấm. |
| Q2 | Nhân viên có quyền đặt lịch cho bất kỳ bệnh nhân nào? | **Có**, tài liệu nói "hỗ trợ đặt" và "xác định đúng bệnh nhân trước khi xử lý" |
| Q3 | Admin có thể xem lịch hẹn không? | **Không trực tiếp** — admin quản lý dữ liệu nền, không xử lý lịch hẹn thay nhân viên (BR01) |
| Q4 | Thời lượng khung giờ mặc định? | **30 phút** (giả định A3), có thể cấu hình trong tương lai |
| Q5 | Lịch làm việc là gì? | **Một bản ghi** xác định BS làm việc ngày nào, từ giờ nào đến giờ nào. Khung giờ là các slot con trong lịch. |
| Q6 | Mối quan hệ admin ↔ nhân viên | **Tách biệt**: Admin quản lý hệ thống (Desktop), nhân viên xử lý nghiệp vụ lịch (Web) |

---

## 7. Rubric đánh giá (Từ tài liệu môn học)

| STT | Tiêu chí | Điểm |
|-----|----------|------|
| 1 | Phân tích & thiết kế hệ thống | 1.0 |
| 2 | Cơ sở dữ liệu | 1.0 |
| 3 | Ứng dụng quản trị Desktop | 1.5 |
| 4 | Ứng dụng Web người dùng | 1.5 |
| 5 | Lập trình C# & kiến trúc | 1.0 |
| 6 | Tích hợp & hoàn thiện | 1.0 |
| 7 | Báo cáo | 1.0 |
| 8 | Demo & vấn đáp | 1.0 |
| 9 | Sáng tạo & mở rộng | 1.0 |
| **Tổng** | | **10.0** |

### Giới hạn điểm
- Không có Web → max 6.0
- Không có Desktop → max 6.0
- Web & Desktop không chung dữ liệu → max 7.0
- Không dùng SQL Server → max 6.5
- Sản phẩm không chạy/demo được → max 5.0
