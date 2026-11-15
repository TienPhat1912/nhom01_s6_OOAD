# Danh mục lưu trữ

## Cấu trúc hiện tại sau khi đổi tên ngày 10/11/2026

Thư mục `project/` đã được đổi tên thành `codes/`. Mã nguồn, scripts, tài liệu Word/PDF và cấu hình cục bộ hiện nằm trong `codes/`; bản lưu trữ tiếp tục ở `archive/`. Các đường dẫn `project/` trong phần lịch sử bên dưới mô tả vị trí trước lần đổi tên này.

## Xóa thư mục codes ngày 10/11/2026

Mã nguồn chạy đã nằm trong `project/`. Các file còn cần giữ đã được chuyển ra ngoài trước khi xóa thư mục `codes`:

| Vị trí cũ | Vị trí sau lần dọn dẹp (trước khi đổi tên) | Số file |
|---|---|---:|
| `codes/README.md` | `project/README.md` | 1 |
| `codes/scripts/README.md` | `project/scripts/README.md` | 1 |
| `codes/docs/` (gồm Word, PDF và tài liệu kỹ thuật) | `project/docs/` | 22 |
| `codes/archive/` | `archive/` | 2360 |
| Cấu hình, thiết lập IDE, log và kết quả kiểm thử còn lại trong `codes/` | `archive/removed-codes/` (giữ đường dẫn tương đối) | 38 |

Đã đối chiếu SHA-256 của 2.422 file sau khi di chuyển, trước khi cập nhật hướng dẫn. Cấu hình demo cũ được lưu riêng tại `archive/removed-codes/demo.local.json`; cấu hình đang dùng trong `project/` được giữ nguyên. Sau đó xóa 1.859 file build/cache trong `codes/src/` và `codes/tests/` (`bin/`, `obj/`, `node_modules/`), giải phóng khoảng 183,91 MiB. Các file này có thể tạo lại bằng setup/build và `npm ci`.

Danh sách file, checksum và kết quả kiểm tra nằm cục bộ tại `codes/.local/codes-removal/` (Git bỏ qua). Dữ liệu trong archive chỉ giữ trên máy; Git theo dõi README và danh mục này.

## Lịch sử lưu trữ ngày 20/10/2026

Các đường dẫn trong bảng dưới tính từ thư mục `codes` ở lần lưu trữ cũ. Hiện toàn bộ thư mục `archive` đã được chuyển ra gốc repository.

| Vị trí cũ | Vị trí lưu trữ | Số file | MiB |
|---|---|---:|---:|
| `src/Hospital.Api/bin` | `archive/build/src/Hospital.Api/bin` | 171 | 42.9 |
| `src/Hospital.Api/obj` | `archive/build/src/Hospital.Api/obj` | 32 | 0.8 |
| `src/Hospital.Application/bin` | `archive/build/src/Hospital.Application/bin` | 5 | 0.1 |
| `src/Hospital.Application/obj` | `archive/build/src/Hospital.Application/obj` | 20 | 0.2 |
| `src/Hospital.Client/bin` | `archive/build/src/Hospital.Client/bin` | 7 | 0.1 |
| `src/Hospital.Client/obj` | `archive/build/src/Hospital.Client/obj` | 20 | 0.1 |
| `src/Hospital.Desktop/bin` | `archive/build/src/Hospital.Desktop/bin` | 11 | 0.4 |
| `src/Hospital.Desktop/obj` | `archive/build/src/Hospital.Desktop/obj` | 22 | 0.4 |
| `src/Hospital.Domain/bin` | `archive/build/src/Hospital.Domain/bin` | 3 | 0 |
| `src/Hospital.Domain/obj` | `archive/build/src/Hospital.Domain/obj` | 18 | 0.1 |
| `src/Hospital.Infrastructure/bin` | `archive/build/src/Hospital.Infrastructure/bin` | 8 | 0.5 |
| `src/Hospital.Infrastructure/obj` | `archive/build/src/Hospital.Infrastructure/obj` | 22 | 0.8 |
| `src/Hospital.Web/bin` | `archive/build/src/Hospital.Web/bin` | 15 | 1.4 |
| `src/Hospital.Web/obj` | `archive/build/src/Hospital.Web/obj` | 136 | 4.4 |
| `tests/Hospital.DesktopSmoke/bin` | `archive/build/tests/Hospital.DesktopSmoke/bin` | 16 | 0.7 |
| `tests/Hospital.DesktopSmoke/obj` | `archive/build/tests/Hospital.DesktopSmoke/obj` | 22 | 0.3 |
| `tests/Hospital.IntegrationTests/bin` | `archive/build/tests/Hospital.IntegrationTests/bin` | 523 | 67.2 |
| `tests/Hospital.IntegrationTests/obj` | `archive/build/tests/Hospital.IntegrationTests/obj` | 22 | 0.9 |
| `tests/Hospital.IntegrationTests/TestResults` | `archive/build/tests/Hospital.IntegrationTests/TestResults` | 11 | 4.1 |
| `tests/Hospital.UnitTests/bin` | `archive/build/tests/Hospital.UnitTests/bin` | 156 | 22 |
| `tests/Hospital.UnitTests/obj` | `archive/build/tests/Hospital.UnitTests/obj` | 21 | 0.4 |
| `tests/Hospital.UnitTests/TestResults` | `archive/build/tests/Hospital.UnitTests/TestResults` | 7 | 0.1 |
| `tests/Hospital.WebTests/bin` | `archive/build/tests/Hospital.WebTests/bin` | 441 | 18.5 |
| `tests/Hospital.WebTests/obj` | `archive/build/tests/Hospital.WebTests/obj` | 22 | 0.3 |
| `tests/Hospital.WebTests/TestResults` | `archive/build/tests/Hospital.WebTests/TestResults` | 5 | 0.1 |
| `tmp` | `archive/previous-runs/tmp` | 93 | 0.9 |
| `tests/browser/artifacts` | `archive/previous-runs/tests/browser/artifacts` | 525 | 50.5 |
| `tests/browser/playwright-report` | `archive/previous-runs/tests/browser/playwright-report` | 1 | 0.5 |
| `tests/browser/test-results` | `archive/previous-runs/tests/browser/test-results` | 1 | 0 |
