# Hospital Appointment System

Hệ thống đặt lịch khám bằng C# gồm **ASP.NET Core Web API**, **Web MVC**, **Windows Forms Admin**, EF Core và SQL Server. Patient/Staff/Doctor dùng Web để đặt, đổi, hủy và tiếp nhận lịch; Admin quản lý người dùng, bác sĩ, chuyên khoa và lịch làm việc qua Desktop.

**Chạy demo trên Windows: clone nhánh `codes` → vào thư mục `codes` → chạy setup → cấu hình SQL → khởi động demo.** Source, migrations, tool manifest, lockfile và tài liệu đều nằm trong thư mục `codes` này. Bản lưu trữ cục bộ nằm trong [`../archive`](../archive/README.md); tên nhánh Git vẫn là `codes`.

## 1. Công cụ cần cài

| Công cụ | Yêu cầu |
|---|---|
| Hệ điều hành | Windows 10/11 x64 để chạy đầy đủ WinForms, API và Web |
| Git | Clone repository và cập nhật mã nguồn |
| .NET | **.NET SDK 9.x cho Windows**, gồm runtime .NET, ASP.NET Core và Windows Desktop 9.x |
| Database | SQL Server/SQL Express; tài khoản có quyền tạo database và chạy migration |
| Tùy chọn | Docker Desktop chạy Linux containers nếu dùng SQL Server container |
| Chỉ khi chạy browser tests | Node.js/npm, Python, `sqlcmd`, Chromium của Playwright |

`global.json` chọn SDK 9.x ổn định (từ 9.0.100, cho phép feature band mới hơn). Chỉ cài SDK 10 không thay thế SDK/runtime 9 mà cấu hình này yêu cầu. Không cần Node, Python, SSMS hay Docker nếu chỉ chạy demo với SQL Express đã cài.

Kiểm tra:

```powershell
git --version
dotnet --version
dotnet --list-runtimes
Get-Service 'MSSQL$SQLEXPRESS'
```

SQL Express mặc định phải ở trạng thái `Running`. Nếu dùng instance khác, cấu hình tên instance tương ứng ở mục 3.

## 2. Clone đúng nhánh và chuẩn bị

Các thay đổi cần được push lên nhánh `codes` trước khi thành viên khác clone được phiên bản mới.

```powershell
git clone --branch codes --single-branch https://github.com/baole09/doan_nhom_sgu26k2_NNLCSharp.git
cd doan_nhom_sgu26k2_NNLCSharp/codes
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/setup.ps1
```

Chạy các lệnh tiếp theo tại **thư mục `codes` chứa `HospitalAppointmentSystem.sln`**, không phải thư mục gốc repository.

Setup tự thực hiện:

1. Tạo `demo.local.json` từ [demo.example.json](demo.example.json) nếu chưa có; giữ nguyên cấu hình hiện có.
2. Kiểm tra đủ runtime 9.x.
3. Restore NuGet bằng `--locked-mode`, restore `dotnet-ef` 9.0.2 từ `.config/dotnet-tools.json` trong `codes`.
4. Build toàn bộ solution, bao gồm Web/API/Desktop và các project kiểm thử.

Chỉ tạo cấu hình mà chưa restore/build:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/setup.ps1 -ConfigOnly
```

## 3. Cấu hình database và cổng

Mở `demo.local.json` sau setup. File này được Git bỏ qua; mỗi thành viên có cấu hình riêng.

```json
{
  "SqlServer": ".\\SQLEXPRESS",
  "DatabaseName": "HospitalDemo",
  "ApiPort": 5014,
  "WebPort": 5174,
  "ConnectionString": ""
}
```

| Trường | Ý nghĩa |
|---|---|
| `SqlServer` | Tên instance SQL Server; ví dụ `.\\SQLEXPRESS` trong JSON hoặc `localhost,14333` với SQL qua TCP |
| `DatabaseName` | Database riêng cho demo, mặc định `HospitalDemo`; bắt đầu bằng chữ, tiếp theo là chữ/số/`_` |
| `ApiPort` | Cổng API, mặc định 5014 |
| `WebPort` | Cổng Web, mặc định 5174; phải khác API |
| `ConnectionString` | Để trống khi dùng Windows Authentication theo `SqlServer`/`DatabaseName`; điền đầy đủ khi dùng SQL Authentication |

Ví dụ dùng SQL login hoặc SQL Server Docker:

```json
{
  "SqlServer": "localhost,14333",
  "DatabaseName": "HospitalDemo",
  "ApiPort": 5014,
  "WebPort": 5174,
  "ConnectionString": "Server=localhost,14333;Database=HospitalDemo;User Id=sa;Password=THAY_BANG_MAT_KHAU_CUA_BAN;TrustServerCertificate=True;"
}
```

Thay mật khẩu trước khi chạy. Với mật khẩu có ký tự đặc biệt trong connection string, dùng `SqlConnectionStringBuilder` như [hướng dẫn database](docs/instructions/demo-guide.md). Không đưa file chứa mật khẩu lên GitHub.

Máy đã chuyển dữ liệu sang container `mssql2022` dùng `127.0.0.1,1433` và database `HospitalDemo`. Xem [hướng dẫn SQL Server Docker](docs/instructions/sql-server-docker.md) để chạy ứng dụng, dùng EF CLI và tìm bản sao lưu/cấu hình riêng.

Thứ tự ưu tiên kết nối:

1. Biến môi trường `ConnectionStrings__DefaultConnection` trong terminal.
2. `ConnectionString` trong `demo.local.json`.
3. Windows Authentication từ `SqlServer` và `DatabaseName`.

Các tham số `-SqlServer`, `-DatabaseName`, `-ApiPort`, `-WebPort` của script ghi đè trường tương ứng trong JSON. Connection string đầy đủ vẫn có ưu tiên cao hơn `SqlServer`/`DatabaseName`.

Có thể chọn file cấu hình khác nằm trong `codes` bằng `-ConfigPath`; tự bảo đảm file chứa thông tin riêng được ignore. Muốn bỏ biến môi trường cũ để dùng JSON: `Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue`.

## 4. Khởi động Web và API

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/demo.ps1 -Action start -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/demo.ps1 -Action status
```

| Địa chỉ mặc định | Công dụng |
|---|---|
| http://localhost:5174 | Web Patient/Staff/Doctor và trang tổng quan Admin |
| http://localhost:5014/swagger | Thử API bằng Swagger |
| http://localhost:5014/health/ready | Kiểm tra API kết nối được database |
| http://localhost:5174/Design | Component gallery, chỉ trong Development |

Script chạy API/Web ở nền, chờ sẵn sàng và ghi log/PID trong `tmp/demo/`. Nếu cổng bận, script báo lỗi; chọn cổng khác trong JSON hoặc dừng phiên demo đang chạy.

API demo tự chạy migrations và seed khi khởi động. Database mới có 4 vai trò, 6 tài khoản, danh mục, bác sĩ, lịch làm việc/khung giờ và lịch hẹn mẫu. Seed chỉ tạo dữ liệu khi chưa có tài khoản; không đặt lại dữ liệu của database đang sử dụng.

### Tài khoản demo

Đây là tài khoản mẫu của database mới, chỉ dùng cho demo cục bộ:

| Vai trò | Username | Mật khẩu |
|---|---|---|
| Admin | `admin` | `Admin@123` |
| Staff | `staff1` | `Staff@123` |
| Doctor | `doctor1`, `doctor2` | `Doctor@123` |
| Patient | `patient1`, `patient2` | `Patient@123` |

Nếu đã đổi mật khẩu trong ứng dụng, seed không khôi phục mật khẩu mẫu. Development sinh khóa JWT ngẫu nhiên mỗi lần API khởi động; đăng nhập lại sau khi restart.

### Dừng và chạy lại

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/demo.ps1 -Action stop
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/demo.ps1 -Action start -SkipBuild
```

Dừng giữ nguyên database. Khi đổi cấu hình hoặc nhận mã nguồn mới, dừng demo, sửa cấu hình/chạy setup rồi khởi động lại. Bỏ `-SkipBuild` để build trước khi chạy; sau thay đổi dependency cần chạy setup để restore.

Muốn bộ dữ liệu demo mới, chọn một `DatabaseName` chưa dùng (và sửa cả tên database trong `ConnectionString` nếu có), rồi khởi động lại. Database trước đó vẫn còn. Lịch seed tính theo ngày tạo; dùng database mới nếu các khung giờ của bản demo cũ đã hết hạn.

## 5. Chạy Windows Forms Admin

Giữ API đang chạy, mở terminal khác tại `codes`:

```powershell
$env:HOSPITAL_API_URL = 'http://localhost:5014/'
dotnet run --project src/Hospital.Desktop --no-build
```

Nếu đổi `ApiPort`, thay URL cho đúng. Đăng nhập `admin` để dùng tổng quan, tài khoản, bác sĩ, chuyên khoa, lịch làm việc, nhật ký và hồ sơ.

Kịch bản nhanh: Patient đặt lịch trên Web → Staff tìm lịch và tiếp nhận → Doctor xem lịch được phân công → Admin quản lý danh mục/lịch làm việc trên Desktop. Xem [kịch bản demo đầy đủ](docs/instructions/demo-guide.md).

## 6. Chạy riêng từng project và migration

Script demo là cách chạy mặc định. Khi cần debug bằng IDE, mở solution trong `codes`; hoặc dùng hai terminal với biến môi trường được đặt riêng trong từng terminal:

```powershell
# Terminal API: đây là cách chạy trực tiếp, không tự đọc demo.local.json.
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = 'Server=.\SQLEXPRESS;Database=HospitalDemo;Trusted_Connection=True;TrustServerCertificate=True;'
$env:Database__SeedDemo = 'true'
dotnet run --project src/Hospital.Api --no-build --launch-profile http

# Terminal Web:
$env:Api__BaseUrl = 'http://localhost:5014/'
dotnet run --project src/Hospital.Web --no-build --launch-profile http
```

Dừng phiên script trước khi chạy riêng để tránh trùng cổng. `Ctrl+C` dừng ứng dụng chạy trong terminal.

Migration dùng connection string của terminal API:

```powershell
dotnet ef database update --project src/Hospital.Infrastructure --startup-project src/Hospital.Api
```

`Database__Initialize` điều khiển chạy migration lúc khởi động; `Database__SeedDemo` điều khiển seed trong Development. JWT cấu hình qua `JwtSettings__Secret` khi cần khóa riêng; môi trường khác Development yêu cầu khóa ít nhất 32 byte. Cấu hình API riêng có thể đặt trong `src/Hospital.Api/appsettings.Local.json` (được ignore). Không đặt thông tin thật vào `appsettings.json`.

## 7. Kiểm thử

Sau setup/build, từ `codes`:

```powershell
# Kiểm tra tạo/giữ cấu hình và từ chối cấu hình sai:
powershell -NoProfile -ExecutionPolicy Bypass -File tests/setup/demo-config.ps1

# Chạy đủ unit, integration và Web tests, bao gồm SQL Server:
$env:HOSPITAL_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet test HospitalAppointmentSystem.sln --no-build --no-restore -m:1 --logger 'trx;LogFileName=verification.trx'

# Kiểm tra vòng đời demo và giữ dữ liệu khi restart:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-demo.ps1
```

SQL tests tự tạo/xóa database riêng có tiền tố kiểm thử; tài khoản cần quyền tương ứng. Không đặt `HOSPITAL_TEST_SQLSERVER` thì nhóm SQL bị bỏ qua. Không xem một lần chạy có skipped SQL tests là nghiệm thu đầy đủ.

Browser/WinForms acceptance cần thêm Node, Python, `sqlcmd`:

```powershell
Push-Location tests/browser
npm ci
npx playwright install chromium
Pop-Location
python scripts/generate_design_tokens.py --check
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase7.ps1 -FinalQa
```

Runner tạo database riêng, khởi động API/Web, chạy browser/Desktop rồi dọn dịch vụ và database thử. Cổng mặc định 5034/5175 cần trống; bài kiểm tra tiếp nhận browser cùng ngày cần chạy trước 22:30 giờ Việt Nam. Log/ảnh mới nằm trong `tests/browser/artifacts/`, `TestResults/`, `tmp/` và được Git bỏ qua.

Báo cáo Phase 3–8 trong `docs/instructions/` là lịch sử nghiệm thu, không phải kết quả tự động áp dụng cho máy vừa clone. Chạy lại bộ kiểm thử tương ứng khi thay đổi code.

## 8. Cấu trúc trong codes

```text
.config/dotnet-tools.json    EF CLI dùng riêng cho thư mục codes
global.json                 SDK .NET 9.x
Directory.Build.props       Sinh NuGet lockfile cho các project
demo.example.json           Mẫu cấu hình an toàn để commit
demo.local.json             Cấu hình riêng của máy, không commit
HospitalAppointmentSystem.sln
src/
  Hospital.Domain/          Entities và enums
  Hospital.Application/     DTO, contracts, validation
  Hospital.Infrastructure/  EF Core, migrations, nghiệp vụ, transaction/outbox
  Hospital.Api/             HTTP API, JWT, phân quyền
  Hospital.Client/          HTTP client dùng chung
  Hospital.Web/             Web MVC
  Hospital.Desktop/         Windows Forms Admin
tests/                      .NET, browser, Desktop smoke, cấu hình setup
scripts/                    Setup, start/stop, kiểm thử, design tokens
design/                     Tokens dùng chung Web/Desktop
docs/                       Tài liệu Word/PDF, yêu cầu, thiết kế và hướng dẫn
```

Tài liệu đồ án, Word/PDF và hướng dẫn nằm trong `docs/`; bản lưu trữ cục bộ ở `../archive/`. Thư mục chạy hiện dùng tên `codes`; dữ liệu lưu trữ đã được tách ra ngoài.

[Danh mục lưu trữ](../archive/MANIFEST.md) ghi vị trí các file đã chuyển. File build/cache cũ được giữ cục bộ trong [archive](../archive/README.md). `bin/`, `obj/` được tạo lại khi build nhưng đã untrack/ignore. Migrations, `packages.lock.json`, lockfile npm và thư viện tĩnh `wwwroot/lib/**/dist` cần được commit để bản clone hoạt động.

Tài liệu: [chỉ mục](docs/README.md), [scripts](scripts/README.md), [kiến trúc](docs/instructions/architecture.md), [database](docs/instructions/database-design.md), [API](docs/instructions/api-documentation.md), [thiết kế giao diện](docs/instructions/phase4-design-system.md).

## 9. Làm việc nhóm và đưa lên GitHub

Từ thư mục gốc repository, kiểm tra đang ở nhánh `codes`. Rà soát `git status` trước khi stage để chỉ ghi nhận những thay đổi muốn chia sẻ:

```powershell
git status
git add -A -- .
git diff --cached --check
git diff --cached --stat
git commit -m "Describe the change"
git push origin codes
```

`codes/.gitignore` bảo vệ cấu hình riêng và file sinh tự động; `.gitignore` ở gốc repository bảo vệ dữ liệu cục bộ trong `archive/`. Không force-add cấu hình riêng, database backup, file build hay archive. Thành viên khác tự chạy setup trong `codes` để tạo `demo.local.json`.

Khi thêm/sửa dependency NuGet, chạy `dotnet restore --force-evaluate` để cập nhật lockfile và commit cùng `.csproj`; sau đó kiểm tra lại `dotnet restore --locked-mode`. Browser dependencies dùng `npm ci` từ lockfile trong `tests/browser`.

## 10. Lỗi thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| Không tìm thấy SDK phù hợp | Cài .NET SDK 9.x; kiểm tra `dotnet --version` ngay trong `codes` |
| Thiếu runtime khi chạy Web/Desktop | Kiểm tra đủ `Microsoft.NETCore.App`, `Microsoft.AspNetCore.App`, `Microsoft.WindowsDesktop.App` 9.x |
| PowerShell chặn script | Dùng lệnh `powershell -NoProfile -ExecutionPolicy Bypass -File ...` như hướng dẫn; chỉ áp dụng cho tiến trình đó |
| Không kết nối được SQL | Kiểm tra service, instance, quyền tạo database; kiểm tra connection string đang được ưu tiên |
| JSON không hợp lệ | Giữ cú pháp JSON; dấu `\` trong tên instance phải viết `\\`; không dùng comment hoặc dấu phẩy cuối |
| Cổng bận / demo đã chạy | Xem `-Action status`, dừng bằng `-Action stop` hoặc chọn cổng khác |
| API/Web thoát khi khởi động | Xem đường dẫn log mà script in ra trong `tmp/demo/` |
| Đăng nhập hết hiệu lực sau restart | Đăng nhập lại; Development dùng khóa JWT theo từng lần chạy |
| Không còn khung giờ mẫu | Dùng database demo mới để seed lịch theo ngày hiện tại |
| NuGet báo lockfile khác dependency | Nếu chủ động sửa package, cập nhật lockfile; nếu chỉ clone, kiểm tra source/lockfile cùng phiên bản và kết nối NuGet |
| Không thấy ảnh/log cũ trong bản clone | Các file này được ignore; chạy lại kiểm thử hoặc lấy bản lưu trữ từ người đã chạy |

Đây là bản demo học tập với tài khoản/dữ liệu mẫu, chưa phải cấu hình triển khai bệnh viện thật.
