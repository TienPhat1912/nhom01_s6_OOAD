# SQL Server Docker cho Hospital Appointment System

Ứng dụng vẫn dùng SQL Server và Entity Framework Core. Chuyển sang Docker chỉ thay máy chủ và cách xác thực; Web và Desktop tiếp tục gọi cùng API.

## Cấu hình trên máy đã chuyển dữ liệu

| Thành phần | Giá trị |
|---|---|
| Container | `mssql2022` |
| SQL Server từ Windows | `127.0.0.1,1433` |
| Database | `HospitalDemo` |
| Volume dữ liệu | `mssql_data`, gắn tại `/var/opt/mssql` |
| API | `http://localhost:5014` |
| Web | `http://localhost:5174` |

Cấu hình riêng nằm trong các file được Git bỏ qua:

- `codes/demo.local.json`: script `demo.ps1` đọc `ConnectionString` từ file này.
- `codes/src/Hospital.Api/appsettings.Local.json`: API đọc kết nối khi chạy trực tiếp bằng IDE hoặc `dotnet run`.
- `codes/.local/docker-sql-credentials.json`: tài khoản `sa` sau khi đặt lại mật khẩu. Mật khẩu trong biến môi trường cũ của container không phải mật khẩu mới.

Ứng dụng dùng SQL login riêng có quyền `db_owner` trong `HospitalDemo` để chạy nghiệp vụ và migrations. Tài khoản `sa` dùng cho quản trị và kiểm thử cần tạo database riêng. Các file cấu hình trên là của từng máy, không được commit.

`ConnectionStrings__DefaultConnection` trong môi trường có thể ghi đè cấu hình file. Nếu terminal vẫn đặt kết nối SQL Express, hãy cập nhật hoặc bỏ biến đó trước khi chạy Docker.

## Chạy ứng dụng

Từ thư mục `codes`:

```powershell
docker start mssql2022
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/demo.ps1 -Action status
# Nếu API/Web chưa chạy:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/demo.ps1 -Action start -SkipBuild
```

`-SkipBuild` dùng khi đã build solution. Lần đầu trên máy khác, chạy `scripts/setup.ps1` và điền kết nối SQL Authentication của máy đó vào hai file cấu hình tương ứng trước khi khởi động.

Desktop dùng API đang chạy:

```powershell
$env:HOSPITAL_API_URL = 'http://localhost:5014/'
dotnet run --project src/Hospital.Desktop --no-build
```

Dừng API/Web bằng `scripts/demo.ps1 -Action stop`. Có thể dừng riêng database bằng `docker stop mssql2022`; volume giữ dữ liệu qua các lần dừng/khởi động. Không xóa volume `mssql_data` nếu còn cần dữ liệu.

## EF migrations từ terminal

`HospitalDbContextFactory` đọc biến môi trường khi chạy EF CLI. Nạp đúng kết nối Docker trước khi dùng `dotnet ef`:

```powershell
$previousConnection = $env:ConnectionStrings__DefaultConnection
try {
    $settings = Get-Content demo.local.json -Raw | ConvertFrom-Json
    $env:ConnectionStrings__DefaultConnection = $settings.ConnectionString
    dotnet ef database update --project src/Hospital.Infrastructure --startup-project src/Hospital.Api
} finally {
    $env:ConnectionStrings__DefaultConnection = $previousConnection
}
```

## Dữ liệu đã chuyển và bản sao lưu

Database `HospitalDemo` được sao lưu bằng `BACKUP DATABASE ... WITH COPY_ONLY, CHECKSUM`, kiểm tra bằng `RESTORE VERIFYONLY`, rồi khôi phục sang Docker với đường dẫn file Linux. Trước khi ứng dụng chạy, 14 bảng với 147 dòng đã khớp số lượng và SHA-256 của toàn bộ nội dung, gồm 7 tài khoản và 2 lịch hẹn. `DBCC CHECKDB` cũng hoàn tất thành công.

Database nguồn `HospitalDemo` tại `.\SQLEXPRESS` vẫn được giữ. `QLCuaHang` và `QLSinhVien` trong Docker không bị ghi đè. Bản sao lưu nguồn, bản sao volume trước khi đổi mật khẩu `sa`, checksum và kết quả kiểm tra nằm trong `codes/.local/docker-transfer/`; `current-run.txt` chỉ đến thư mục của lần chuyển này. Thông báo nền có thể tiếp tục cập nhật outbox/thông báo khi ứng dụng chạy; kết quả đối chiếu dữ liệu được lưu tại thời điểm vừa khôi phục.

Muốn quay lại SQL Express, dừng API/Web, đổi kết nối trong cả hai file cấu hình cục bộ sang `.\SQLEXPRESS`/`HospitalDemo` với Windows Authentication, rồi khởi động lại. Database nguồn chỉ có dữ liệu tới thời điểm sao lưu; dữ liệu mới phát sinh trong Docker cần được chuyển riêng nếu muốn mang về nguồn.

Tham khảo: [Microsoft — khôi phục SQL Server backup trong container](https://learn.microsoft.com/en-us/sql/linux/migrate/tutorial-restore-backup-sql-server-container?view=sql-server-ver16), [Microsoft — mật khẩu khi tái sử dụng volume](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-docker-container-troubleshooting?view=sql-server-ver16).
