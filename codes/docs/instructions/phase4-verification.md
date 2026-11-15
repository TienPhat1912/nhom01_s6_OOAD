# Kiểm chứng Phase 4 — 17/10/2026

Phạm vi là design system và màn hình nền tảng nối dữ liệu thật. Backend Phase 3 giữ nguyên hợp đồng nghiệp vụ. Không đánh dấu các luồng Phase 5–7 đã hoàn tất.

## Kết quả

| Kiểm tra | Kết quả |
|---|---|
| Build solution | 0 warning, 0 error |
| Tokens JSON → CSS/C# | `--check` đồng bộ |
| Unit + backend integration | 65 passed, gồm 7 SQL Server tests |
| MVC Web integration | 6 passed |
| Chromium Playwright | 8 passed |
| Axe | Không có violation thuộc các tags WCAG 2 A/AA, 2.1 AA đã chọn trên trang kiểm tra |
| WinForms smoke | Đạt: validation, Admin API login, 3 danh sách thật, tìm kiếm rỗng và render 2 kích thước |

Môi trường: Windows, SDK .NET 10.0.400, runtime 9.0.1, SQL Server 2022 Express `.\SQLEXPRESS`, Node 22.19.0, Playwright 1.64.1, axe-core/playwright 4.14.0, Chromium build 1248 cài tại máy kiểm chứng.

`Hospital.WebTests` dùng backend giả có chủ đích để kiểm soát lỗi/thu hồi phiên. 6 test xác nhận: CSRF và redirect nội bộ; phân quyền/thu hồi phiên; lỗi 503 kèm trace và phục hồi cùng cookie; validation/whitelist form hồ sơ; logout POST+CSRF; gallery trả 404 ngoài Development.

Playwright dùng API thật và database riêng `HospitalPhase4Ui_<timestamp>`, có 6 tài khoản, 2 bác sĩ, 5 chuyên khoa và lịch hẹn seed. Kiểm tra trang công khai, dashboard/danh sách/hồ sơ của 4 vai trò, chi tiết bác sĩ/lịch hẹn, empty search, trang trái quyền, mobile menu và native dialog. Form hồ sơ lưu lại giá trị hiện có trong database thử nghiệm. Chiều rộng: **320, 360, 430, 768, 1440px**; vùng bảng tự cuộn, không tràn ngang toàn trang. Kiểm tra không có JavaScript page error trên các trang theo vai trò.

WinForms smoke chạy message loop Windows thật với opacity=0, không bật cửa sổ tương tác trước mặt người dùng. Form được render bằng DrawToBitmap, kiểm tra nhãn/validation, dữ liệu grid nhận qua API và empty state. Đã xem ảnh login, login tối thiểu, bảng tài khoản, chuyên khoa ở cửa sổ tối thiểu, Web login và dashboard desktop/mobile. Đây chưa phải kiểm thử GUI thủ công hoặc kiểm thử tự động toàn bộ thao tác chuột/bàn phím Windows.

Các lỗi tìm được và đã sửa: vùng chữ ẩn của bảng làm trang tràn ngang mobile; dispose WinForms lần thứ hai làm lỗi CancellationTokenSource; màu header được chọn trong grid thiếu tương phản; chữ thương hiệu/tiêu đề bị cắt ở cửa sổ nhỏ. Smoke harness dùng Application.Run và bật phát hiện cross-thread access để kiểm tra async UI đúng vòng lặp Windows.

## Chạy lại .NET tests

Từ thư mục `codes`, dừng app đang chạy trước khi build:

```powershell
dotnet restore HospitalAppointmentSystem.sln
dotnet build HospitalAppointmentSystem.sln --no-restore --no-incremental
python scripts/generate_design_tokens.py --check
$env:HOSPITAL_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet test HospitalAppointmentSystem.sln --no-build --no-restore --logger 'trx;LogFileName=verification.trx'
```

71 test phải passed, không skipped. Nếu không đặt `HOSPITAL_TEST_SQLSERVER`, 7 test SQL Server sẽ skipped. DesktopSmoke là chương trình smoke riêng, không được thực thi bởi `dotnet test`.

Repository hiện còn theo dõi một số `bin/obj` cũ. Dùng `--no-incremental` như trên trước `--no-build` để luôn chạy đúng source hiện tại. Bản bàn giao giữ các thay đổi source, không đưa output build vào thay đổi Phase 4.

## Chạy lại trình duyệt và WinForms smoke

Terminal 1, khởi tạo **database thử nghiệm riêng**, không trỏ vào HospitalDb:

```powershell
$phase4Database = 'HospitalPhase4Ui_' + [guid]::NewGuid().ToString('N')
$env:ConnectionStrings__DefaultConnection = "Server=.\SQLEXPRESS;Database=$phase4Database;Trusted_Connection=True;TrustServerCertificate=True;"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Database__SeedDemo = 'true'
dotnet run --project src/Hospital.Api --no-build --no-launch-profile --urls http://localhost:5034
```

Terminal 2:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Api__BaseUrl = 'http://localhost:5034/'
dotnet run --project src/Hospital.Web --no-build --no-launch-profile --urls http://localhost:5175
```

Terminal 3, từ `codes`:

```powershell
Push-Location tests/browser
npm ci
npx playwright install chromium
$env:HOSPITAL_WEB_URL = 'http://localhost:5175'
npm test
Pop-Location
$env:HOSPITAL_API_URL = 'http://localhost:5034/'
dotnet run --project tests/Hospital.DesktopSmoke --no-build
```

Có thể dùng Chromium đã cài bằng biến `PLAYWRIGHT_CHROMIUM_PATH` trỏ đến executable. Mỗi suite đăng nhập 4 tài khoản; API giới hạn 10 lần/phút/IP, nên đợi hết cửa sổ rate limit khi chạy liên tiếp nhiều suite. Không vô hiệu rate limiter để test. `maxFailures: 1` dừng sớm để tránh đăng nhập lặp khi worker khởi động lại sau lỗi.

Ảnh chụp nằm ở `tests/browser/artifacts`; HTML report ở `tests/browser/playwright-report`; trace khi lỗi ở `tests/browser/test-results`. Các thư mục này được gitignore vì có thể chứa cookie/ảnh hồ sơ. Token không được in ra console. Sau thử nghiệm, dừng hai terminal bằng Ctrl+C; chỉ xóa đúng database có tên `$phase4Database` vừa tạo nếu không cần giữ lại.

## Giới hạn

Chưa kiểm chứng screen reader, Safari/Firefox, high contrast, nhiều DPI/màn hình và GUI WinForms tương tác toàn diện. Axe không chứng minh tuân thủ WCAG đầy đủ. Chưa chạy luồng đặt/đổi/hủy Web hoặc CRUD Desktop vì thuộc Phase 5–6. Báo cáo này không thay thế kiểm thử tích hợp toàn hệ thống Phase 7.

Sau lần kiểm chứng cuối, đã dừng API/Web thử nghiệm và xóa riêng database `HospitalPhase4Ui_1792246385267` do lượt này tạo. Database `HospitalDb` không được dùng để chạy browser/desktop smoke.
