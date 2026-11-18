# Demo OOAD bằng Docker Compose

Compose ở thư mục gốc chạy ba dịch vụ: SQL Server 2022 Developer, ASP.NET Core API và Web MVC. Build dùng .NET 9 theo target framework hiện tại. Máy demo chỉ cần Docker Desktop chạy Linux containers cho ba dịch vụ này; không cần cài SQL Server hoặc .NET trên host. WinForms Admin vẫn chạy trên Windows, cần SDK/runtime .NET 9 tương ứng.

Đây là cấu hình demo cục bộ dùng Development để bật Swagger và seed. API/Web chỉ publish cổng trên 127.0.0.1; SQL Server chỉ nằm trong mạng Compose. Các chức năng OOAD còn thiếu được theo dõi trong `OOAD_DEMO_PLAN.md`; đóng gói không thay đổi nghiệp vụ hiện tại.

## Chạy lần đầu

Chạy tại thư mục gốc chứa `compose.yaml`:

```powershell
Copy-Item .env.example .env
docker compose config --quiet
docker compose up -d --build --wait --wait-timeout 360
docker compose ps
```

Chỉ copy `.env` khi chưa có file. `.env` được Git bỏ qua. Mật khẩu mẫu chỉ dành cho demo; có thể thay trước khi tạo database lần đầu. Mật khẩu SQL cần đủ mạnh; với cấu hình connection string hiện tại nên dùng ký tự ASCII chữ, số, `!`, `@`, `_`, tránh `;`, dấu nháy, `$`. JWT secret phải ít nhất 32 byte. API_PORT và WEB_PORT phải khác nhau và không trùng dịch vụ đang chạy từ script demo cũ.

Lần đầu cần tải image và NuGet nên có thể mất vài phút. SQL healthcheck phải thành công trước khi API chạy migration/seed; Web đợi API sẵn sàng. Build/pull image có thể lâu hơn thời gian chờ readiness.

| Địa chỉ mặc định | Chức năng |
|---|---|
| http://localhost:5174 | Web |
| http://localhost:5014/swagger | Swagger API |
| http://localhost:5014/health/ready | Kiểm tra API kết nối database |

Web gọi API qua `http://api:8080/` trong mạng Docker. Trình duyệt và Desktop dùng cổng host; không dùng hostname `api` trên Windows.

Tài khoản database mới: `admin` / `Admin@123`, `staff1` / `Staff@123`, `doctor1` và `doctor2` / `Doctor@123`, `patient1` và `patient2` / `Patient@123`. Seed không đặt lại tài khoản/database đã có dữ liệu. Dữ liệu mẫu hiện tại dùng ngày lúc seed; trước buổi demo cần chuẩn bị lại lịch nếu slot đã hết hạn.

## WinForms Admin

Giữ Compose đang chạy; từ thư mục gốc mở terminal khác:

```powershell
$env:HOSPITAL_API_URL = 'http://localhost:5014/'
dotnet run --project codes/src/Hospital.Desktop
```

Đổi URL nếu sửa API_PORT. Đăng nhập bằng Admin. WinForms không được đưa vào các Linux container của Compose.

## Dừng và chạy lại

```powershell
docker compose stop
docker compose start --wait --wait-timeout 360
```

Khi sửa code hoặc Compose:

```powershell
docker compose up -d --build --wait --wait-timeout 360
```

`docker compose down` gỡ container/network nhưng giữ volume database và khóa cookie Web. Không dùng `down -v` khi cần giữ dữ liệu. Không đổi mật khẩu SQL trong `.env` rồi kỳ vọng tài khoản sa trong volume cũ tự đổi theo; cần đổi mật khẩu trong SQL Server hoặc dùng bộ dữ liệu mới có chủ đích. Giữ JWT_SECRET ổn định để phiên API không mất hiệu lực chỉ vì tạo lại container.

Nếu muốn database demo mới mà vẫn giữ bộ cũ, dùng project name khác, ví dụ `docker compose -p ooad-rehearsal up -d --build --wait --wait-timeout 360`; dừng project đang dùng cùng cổng trước. Dùng lại `-p ooad-rehearsal` cho các lệnh quản lý bộ mới.

## Xem lỗi

```powershell
docker compose ps -a
docker compose logs --tail 100 db api web
```

- Không kết nối Docker engine: mở Docker Desktop, chọn Linux containers rồi thử lại.
- Cổng đã dùng: dừng phiên demo cũ hoặc thay API_PORT/WEB_PORT trong `.env` rồi chạy lại `up`.
- SQL unhealthy: xem log, kiểm tra mật khẩu và bộ nhớ Docker. SQL Server cần tối thiểu 2 GB RAM; cần cấp thêm tài nguyên cho build/API/Web.
- API lỗi khởi động: xem log migration/connection string/JWT, không xóa volume để che lỗi migration.
- Web không đăng nhập được: kiểm tra API readiness và log; API nội bộ phải dùng `http://api:8080/`.
- Muốn xem SQL bằng công cụ ngoài: cấu hình hiện không publish cổng SQL; dùng `docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C` và nhập mật khẩu khi được hỏi.

## Kiểm tra trước buổi demo

1. `docker compose ps` có đủ ba dịch vụ healthy; readiness API trả thành công.
2. Đăng nhập Patient, tra cứu và đặt lịch; Staff tìm đúng mã lịch; Doctor xem lịch của mình; Desktop Admin kết nối API.
3. Khởi động lại stack, xác nhận lịch đã đặt và thông báo còn tồn tại.
4. Chạy hồi quy chức năng theo kế hoạch OOAD trên database kiểm thử riêng.

Tham khảo: [thứ tự khởi động và healthcheck của Compose](https://docs.docker.com/compose/how-tos/startup-order/), [SQL Server trong Docker](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-docker-container-deployment?view=sql-server-ver17).
