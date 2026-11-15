# Script của dự án

Các script nằm trong [`codes/scripts`](./). Chạy các lệnh từ thư mục `codes`, không phải thư mục gốc repository.

| Script | Công việc |
|---|---|
| `setup.ps1` | Tạo cấu hình riêng, restore đúng lockfile và build toàn bộ solution; `-ConfigOnly` chỉ tạo/kiểm tra cấu hình |
| `demo-config.ps1` | Đọc và kiểm tra cấu hình JSON dùng chung |
| `demo.ps1` | `-Action start/stop/status` để chạy/dừng/kiểm tra API và Web; giữ database |
| `verify-demo.ps1` | Kiểm tra start/stop, PID, seed và giữ dữ liệu sau restart trên database riêng |
| `verify-phase7.ps1` | Kiểm thử tích hợp .NET, SQL, Web và WinForms; `-FinalQa` bật bộ nghiệm thu giao diện |
| `generate_design_tokens.py` | Sinh CSS/WinForms tokens; `--check` kiểm tra file đã sinh còn khớp |

`demo.local.json` chỉ nằm trên máy của mỗi thành viên. `ConnectionStrings__DefaultConnection` từ môi trường được ưu tiên hơn cấu hình JSON. Log/PID mới nằm trong `tmp/`; kết quả nghiệm thu nằm trong `TestResults/` và `tests/browser/artifacts/`. Các file này không được commit.

Xem [README](../README.md) để cài đặt và chọn cổng/database.
