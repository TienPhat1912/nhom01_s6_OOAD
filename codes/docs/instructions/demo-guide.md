# Hướng dẫn cài đặt và demo trên Windows 11

Cập nhật 20/10/2026. Chạy các lệnh PowerShell tại thư mục `codes` chứa `HospitalAppointmentSystem.sln`. API và Web chạy trên máy cục bộ; WinForms cần Windows. Không cần dịch vụ cloud hoặc tài khoản trả phí.

## 1. Chuẩn bị

- Cài Git và .NET SDK 9.x cho Windows để build `net9.0` / `net9.0-windows`; `global.json` chọn SDK 9. Máy kiểm chứng dùng SDK 9.0.102 và runtime 9.0.1. Kiểm tra `dotnet --list-sdks` và `dotnet --list-runtimes`: cần runtime 9 cho `Microsoft.NETCore.App`, `Microsoft.AspNetCore.App`, `Microsoft.WindowsDesktop.App`. SDK mới hơn không tự thay thế runtime mà ứng dụng yêu cầu. Xem [cài .NET trên Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows) và [bộ cài .NET 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).
- SQL Server: chọn SQL Express trên Windows hoặc SQL Server Docker ở mục 2. Có thể dùng SSMS để xem dữ liệu. Tài khoản cần quyền tạo database và áp dụng migration cho lần khởi tạo.
- Chạy kiểm thử browser cần Node/npm, Python và `sqlcmd`; chỉ chạy demo không cần ba công cụ này.
- Clone nhánh `codes` của repository rồi làm việc trong thư mục `codes`. Thư mục này đã có `.config/dotnet-tools.json` riêng cho `dotnet-ef` 9.0.2, `global.json` chọn SDK 9 và lockfile NuGet. Xem [README hiện tại](../../README.md) để dùng `scripts/setup.ps1` và cấu hình `demo.local.json`.

```powershell
dotnet restore HospitalAppointmentSystem.sln --locked-mode
dotnet tool restore
dotnet build HospitalAppointmentSystem.sln --no-restore --no-incremental --disable-build-servers -m:1
```

## 2. Kết nối database

### SQL Express với Windows Authentication — đã kiểm chứng

```powershell
Get-Service 'MSSQL$SQLEXPRESS'
$env:ConnectionStrings__DefaultConnection = 'Server=.\SQLEXPRESS;Database=HospitalDemo;Trusted_Connection=True;TrustServerCertificate=True;'
```

Service phải ở trạng thái Running. Nếu instance khác, thay phần `Server`. Chọn database riêng cho demo; không đổi tên database đang dùng cho công việc khác.

### SQL Server Docker hiện có

Mở Docker Desktop ở chế độ Linux containers rồi kiểm tra tên/cổng đã được cấp:

```powershell
docker ps -a --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
```

Dùng tên container và cổng host hiện có. Nếu container của bạn đang dừng, chạy `docker start <ten-container>`. Giữ nguyên container/volume đang chứa dữ liệu; không cần khởi tạo lại. Ví dụ ánh xạ `127.0.0.1:14333->1433/tcp` thì ứng dụng dùng `localhost,14333`.

### Tạo container riêng nếu chưa có

Kiểm tra cổng 14333 bằng `Get-NetTCPConnection -State Listen -LocalPort 14333 -ErrorAction SilentlyContinue`. Nếu đã có dịch vụ, chọn cổng khác và sửa đồng bộ connection string. Chọn tên container/volume chưa được dùng trong `docker ps -a` và `docker volume ls`.

```powershell
$demoSaSecret = Read-Host 'Mat khau sa cho SQL demo (du manh)' -AsSecureString
$env:MSSQL_SA_PASSWORD = [System.Net.NetworkCredential]::new('', $demoSaSecret).Password
docker run --name hospital-demo-sql -e ACCEPT_EULA=Y -e MSSQL_PID=Developer -e MSSQL_SA_PASSWORD -p 127.0.0.1:14333:1433 -v hospital-demo-data:/var/opt/mssql -d mcr.microsoft.com/mssql/server:2022-latest
Remove-Item Env:MSSQL_SA_PASSWORD
docker logs hospital-demo-sql --tail 30
```

Chờ SQL báo sẵn sàng nhận kết nối. Volume giữ database khi container dừng. Cách ánh xạ cổng và lưu dữ liệu theo [hướng dẫn container SQL Server của Microsoft](https://learn.microsoft.com/en-us/sql/linux/containers/configure?view=sql-server-ver16). Đường Docker được tài liệu hóa, chưa chạy thử trong lượt nghiệm thu này.

Cấu hình SQL Authentication bằng builder để mật khẩu chứa ký tự đặc biệt được escape đúng; dùng cả cho container hiện có:

```powershell
$demoSqlSecret = Read-Host 'Mat khau SQL demo' -AsSecureString
$demoSql = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$demoSql['Data Source'] = 'localhost,14333'
$demoSql['Initial Catalog'] = 'HospitalDemo'
$demoSql['User ID'] = 'sa'
$demoSql['Password'] = [System.Net.NetworkCredential]::new('', $demoSqlSecret).Password
$demoSql['TrustServerCertificate'] = $true
$env:ConnectionStrings__DefaultConnection = $demoSql.ConnectionString
```

Đây là cấu hình local demo. Không ghi mật khẩu thật vào source, ảnh hoặc lệnh được chia sẻ. Có thể dùng SQL login riêng có quyền thích hợp thay `sa`.

## 3. Migration và seed

Sau khi đặt connection string ở cùng cửa sổ PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project src/Hospital.Infrastructure --startup-project src/Hospital.Api
$env:Database__Initialize = 'true'
$env:Database__SeedDemo = 'true'
```

Development cũng tự migrate khi khởi động nếu `Database__Initialize=true`. Seed chạy khi database chưa có Users; không đặt lại tài khoản, mật khẩu hoặc lịch hẹn cũ. Database mới có 4 vai trò, 6 tài khoản, 2 bệnh nhân, 5 chuyên khoa, 2 bác sĩ, 14 ca/98 khung trong 7 ngày từ ngày seed và 2 lịch Confirmed ngày mai. Chi tiết nằm trong [database-design.md](database-design.md).

## 4. Khởi động, trạng thái và dừng

Cách thuận tiện sau khi restore/build:

```powershell
powershell -NoProfile -File scripts/demo.ps1 -Action start -SkipBuild
powershell -NoProfile -File scripts/demo.ps1 -Action status
```

Script ưu tiên `ConnectionStrings__DefaultConnection` trong môi trường, sau đó `ConnectionString` trong `demo.local.json`. Khi cả hai trống, dùng `SqlServer`/`DatabaseName` trong JSON (mặc định Windows Authentication, `.\SQLEXPRESS`, `HospitalDemo`). Tham số `-SqlServer`/`-DatabaseName` ghi đè các trường tương ứng trong JSON, nhưng không ghi đè connection string đầy đủ. Bỏ `-SkipBuild` để build lại trước khi chạy.

Script khởi động API rồi Web ở nền, chờ readiness, ghi PID/thời điểm bắt đầu và log trong `tmp/demo`. Nếu cổng đã bận, script báo lỗi và giữ nguyên dịch vụ đang chạy. Có thể chọn `-ApiPort 5016 -WebPort 5176`; dùng cùng `-StateDirectory` khi start/status/stop nếu đổi thư mục lưu phiên.

Mở [Web](http://localhost:5174), [Swagger](http://localhost:5014/swagger), [API readiness](http://localhost:5014/health/ready). Với cổng khác, sửa URL tương ứng. Mở WinForms trong PowerShell riêng:

```powershell
$env:HOSPITAL_API_URL = 'http://localhost:5014/'
dotnet run --project src/Hospital.Desktop --no-build
```

Đăng nhập Desktop bằng Admin. Dừng demo sau khi đóng Desktop:

```powershell
powershell -NoProfile -File scripts/demo.ps1 -Action stop
```

Stop chỉ dừng tiến trình có PID, tên assembly và thời điểm bắt đầu khớp phiên đã lưu. Database được giữ nguyên.

Nếu muốn chạy từng host để xem log trực tiếp, không dùng đồng thời với phiên script trên cùng cổng:

```powershell
# Cua so 1, sau khi dat connection string
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Database__SeedDemo = 'true'
dotnet run --project src/Hospital.Api --no-build --launch-profile http
```

```powershell
# Cua so 2
$env:Api__BaseUrl = 'http://localhost:5014/'
dotnet run --project src/Hospital.Web --no-build --launch-profile http
```

## 5. Tài khoản và kịch bản trình diễn

| Vai trò | Username | Mật khẩu development |
|---|---|---|
| Admin | `admin` | `Admin@123` |
| Staff | `staff1` | `Staff@123` |
| Doctor | `doctor1`, `doctor2` | `Doctor@123` |
| Patient | `patient1`, `patient2` | `Patient@123` |

Mở các browser profile/cửa sổ riêng cho Patient, Staff và Doctor; các tab chung profile dùng chung cookie. Desktop có phiên Admin riêng. Kịch bản khoảng 15–20 phút:

1. **Admin Desktop:** xem tổng quan; tạo chuyên khoa mã mới, hồ sơ bác sĩ mới, chọn chuyên khoa chính; cấp tài khoản Doctor gắn hồ sơ đó. Tạo ca ngày mai 08:00–10:00; ca chia bốn khung 30 phút. Xem chi tiết để ghi nhận slot, tên bác sĩ và ngày.
2. **Patient Web:** đăng nhập, tải lại danh mục; bác sĩ/chuyên khoa/ca vừa tạo xuất hiện từ API. Chọn khung 08:00, xem lại và xác nhận; ghi mã hẹn, xem/in phiếu. Desktop tải lại thấy khung đã được đặt; thử đóng khung nhận từ chối.
3. **Doctor Web:** đăng nhập tài khoản vừa cấp; xem lịch làm việc và chi tiết đúng mã hẹn. Patient khác không thấy lịch này trong danh sách của mình.
4. **Đổi và hủy:** Patient đổi sang 08:30 ngày mai (còn ít nhất 2 giờ), xem lịch sử và thông báo, rồi hủy. Desktop tải lại thấy chỗ được giải phóng; đóng khung trống với lý do. Patient tải lại không chọn được khung đã đóng.
5. **Staff đặt hộ:** tìm `patient1` theo tên/số điện thoại, đối chiếu hồ sơ, đặt khung khác; xem lịch hẹn và người đặt hộ. Staff đổi/hủy trước giờ bắt đầu cần lý do.
6. **Tiếp nhận cùng ngày:** Admin tạo ca cho bác sĩ mới vào hôm nay, giờ bắt đầu còn ở tương lai, không chồng ca khác. Patient đặt, Staff tìm mã, đối chiếu bệnh nhân rồi tiếp nhận. Patient/Doctor tải lại thấy CheckedIn. Admin bị chặn đóng slot đang sử dụng. Nếu quá muộn trong ngày để tạo khung hợp lệ, trình diễn bước này hôm sau hoặc dùng bằng chứng kiểm thử; không đổi đồng hồ máy.
7. **Quy tắc lỗi:** tạo ca chồng lấn bị từ chối; thử khóa Admin hoạt động cuối cùng bị từ chối. Hai Patient chọn cùng slot từ hai phiên, xác nhận gần nhau: một thành công; người còn lại cần chọn khung khác. Không thể NoShow trước khi hết giờ; kiểm thử tự động có đồng hồ điều khiển để kiểm tra đầy đủ biên thời gian.
8. **Quản trị và cá nhân:** xem audit trước/sau; chọn danh mục không còn lịch hiệu lực để minh họa ngừng hoạt động và giữ lịch sử; thử sửa hồ sơ. Đổi mật khẩu ở cuối demo vì sẽ thu hồi phiên; mật khẩu seed không tự khôi phục khi restart.

Thông báo là trong ứng dụng, không gửi email/SMS. Nhắc lịch chạy khi đến hạn trước giờ khám một giờ. CheckedIn nghĩa là đã tiếp nhận, không có quy trình bệnh án hoặc thanh toán.

## 6. Chạy lại sau khi tắt máy

1. Bật SQL Express hoặc Docker Desktop và container chứa database. Giữ nguyên database/volume.
2. Mở PowerShell tại `codes`; đặt lại connection string nếu dùng Docker/instance tùy chỉnh (biến môi trường của phiên cũ không còn).
3. Chạy `scripts/demo.ps1 -Action start -SkipBuild`, mở Web và Desktop như mục 4. Nếu source/dependency thay đổi, restore/build lại.
4. Đăng nhập lại: khóa JWT development thay đổi theo process. Script bỏ qua PID cũ không khớp thời điểm bắt đầu.
5. Lịch seed không tự chuyển sang tuần hiện tại. Admin tạo ca mới cho ngày phù hợp; muốn một bộ seed mới, dùng tên database demo mới và cấu hình lại connection string.

Đã kiểm chứng stop/start giữ nguyên Id của 6 Users và 2 Appointments trên SQL Express. Chưa thực hiện tắt/bật máy vật lý trong lượt nghiệm thu.

## 7. Kiểm chứng và xử lý sự cố

Nghiệm thu đầy đủ và gallery: [phase8-verification.md](phase8-verification.md). Kiểm tra riêng vòng đời demo trên database tạm, tự dọn sau khi chạy:

```powershell
powershell -NoProfile -File scripts/verify-demo.ps1
```

| Hiện tượng | Cách xử lý |
|---|---|
| Thiếu framework/runtime | Kiểm tra `dotnet --list-runtimes`, cài runtime 9 tương ứng Web/Desktop rồi build lại |
| Không kết nối SQL / readiness lỗi | Kiểm tra service/container, instance/cổng, credential/quyền; xem log API trong thư mục mà script in ra |
| Cổng đã dùng | Xem `Get-NetTCPConnection -State Listen -LocalPort <cong>`; dùng cổng khác hoặc dừng đúng phiên demo bằng script |
| Không đăng nhập được sau restart | Đăng nhập lại; nếu đã đổi mật khẩu phải dùng mật khẩu mới |
| 429 khi đăng nhập liên tục | Chờ ít nhất 60 giây; hạn mức auth 10 request/phút/IP áp dụng cả các phiên Web qua cùng server |
| 409 khi đổi/hủy | Tải lại lịch để lấy phiên bản mới, xem thay đổi rồi xác nhận lại |
| Mất kết nối ngay sau xác nhận đặt | Tra cứu danh sách/mã hẹn hoặc thử lại cùng trang để giữ RequestKey; chưa tạo yêu cầu mới khi chưa rõ kết quả |
| Không có slot tương lai | Kiểm tra ngày, trạng thái bác sĩ/chuyên khoa/ca; tạo ca tương lai trên Desktop |
| PowerShell chặn script | Chạy lệnh bằng `powershell -NoProfile -ExecutionPolicy Bypass -File ...` cho tiến trình đó; không cần đổi policy toàn máy |

Đọc thêm [kiến trúc](architecture.md), [giải thích kỹ thuật](technical-explanation.md) và [ma trận 28 UC](usecase-coverage.md).
