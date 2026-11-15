# Phase 8 — Rà soát cuối và bàn giao

Cập nhật 19/10/2026. Phạm vi: hoàn thiện giao diện, chạy lại nghiệp vụ trên dữ liệu thật, chuẩn bị vận hành demo và đồng bộ tài liệu với mã nguồn. Không thêm nghiệp vụ ngoài 28 UC, không thêm migration.

## 1. Thay đổi hoàn tất

- Menu mobile có nút đóng, khóa tương tác nền bằng `inert`, giữ focus khi đóng bằng nút/Escape và bỏ trạng thái mở khi đổi breakpoint hoặc quay lại trang. Màn hình thấp vẫn cuộn đến đăng xuất.
- Nút thao tác trong bảng đạt 44×44 px; form footer xuống dòng khi thiếu chỗ; tiêu đề dài không gây tràn trang. Bảng tìm bệnh nhân dùng cùng định dạng bảng với các danh sách khác, tách rõ tên/mã bệnh nhân.
- Liên kết thử lại trên trang lỗi giữ PathBase, path và query, tránh làm mất context của quy trình đặt lịch.
- Bổ sung gallery Web/WinForms từ API/SQL thật, kiểm tra responsive, axe, menu, modal và bản in phiếu hẹn.
- Script `demo.ps1` khởi động API/Web theo thứ tự, chờ readiness, ghi log, kiểm tra cổng và nhận diện PID/thời điểm bắt đầu khi dừng. Stop/start giữ database; `verify-demo.ps1` tự kiểm chứng bằng database tạm riêng.
- README, kiến trúc, database, Demo Guide, giải thích kỹ thuật tiếng Việt và ma trận UC đã được đối chiếu với sản phẩm triển khai.

## 2. Kết quả kiểm chứng

Nghiệm thu đầy đủ ngày 18/10/2026, run `0d3b172b642e41a2950f5e750fd82157`, kết thúc exit 0:

| Nhóm | Kết quả |
|---|---|
| Build solution | 0 warning, 0 error |
| .NET | 102 passed, 0 failed, 0 skipped: 8 unit + 88 integration + 6 Web; gồm 28 SQL Server tests |
| Tích hợp Desktop → Web → SQL | 1 Playwright test đạt; tạo danh mục/ca, đặt, tiếp nhận, cạnh tranh, idempotency, đổi/hủy, thông báo, đóng slot |
| Browser hồi quy | 11 tests đạt |
| Final UI | 7 Playwright tests đạt: 6 Chromium và 1 chạy gallery WinForms |
| Tổng Playwright | 19 tests, gồm 18 browser và 1 Desktop gallery; không cộng thành 19 browser thuần |
| Desktop hồi quy | 89 assertions đạt bằng native WinForms message loop/API/SQL |
| Desktop gallery | 22 màn hình/form × 2 kích thước, 40 assertions nút thao tác không bị cắt |
| SQL đối chiếu | 3 lịch của bác sĩ fixture, 3 thông báo lifecycle, đúng lịch sử/trạng thái; không có slot bị hai lịch hiệu lực chiếm |
| Design tokens | Đồng bộ CSS/C# với nguồn JSON |
| Dọn môi trường | API/Web thử nghiệm đã dừng; RemainingTestDatabase=0 |

Bản sửa cuối cho bảng bệnh nhân được build và chạy lại các nhóm UI/tích hợp ngày 19/10/2026, run `f409ba2462ce475b8462a861310d0555`, bằng `-FinalQa -SkipDotNet`, kết thúc exit 0: build 0 warning/error, đủ 19 Playwright tests, 89 + 40 assertions WinForms đạt và RemainingTestDatabase=0. Nhóm .NET dùng kết quả đầy đủ ngày 18/10; không thay đổi backend hoặc logic .NET từ lượt đó.

Vòng đời demo ngày 19/10/2026, run `c8c9f95bb7e04696a52ea3204e7304fe`, exit 0: start/readiness/status, từ chối start trùng, bỏ qua state có PID nhưng thời điểm bắt đầu sai, stop/start và giữ nguyên Id của 6 Users + 2 Appointments. Database `HospitalPhase8Demo_<run>` đã được dọn. Không dùng database `HospitalDb` cho các thao tác thử nghiệm.

## 3. Rà soát trực quan

Đã mở ảnh ở hai kích thước và contact sheet để xem layout, typography, màu, khoảng cách, button/form/navigation. Đã phát hiện và sửa bảng tìm bệnh nhân thiếu class định dạng chung; kiểm tra lại ảnh sau sửa.

| Phân hệ | Màn hình/trạng thái trong gallery |
|---|---|
| Public | Trang chủ, quyền riêng tư, đăng nhập, đăng ký, thư viện component |
| Patient | Dashboard, hồ sơ, đổi mật khẩu, lịch hẹn, danh mục/chuyên khoa/chi tiết bác sĩ, thông báo, chi tiết lịch, phiếu, hủy không hợp lệ, kết quả rỗng |
| Đặt lịch/lỗi | Chọn chuyên khoa, bác sĩ, slot, xem lại, xác nhận/hủy modal; context hết hạn, 403, 404 |
| Staff | Dashboard, hồ sơ, mật khẩu, danh sách/chi tiết lịch, danh mục bác sĩ/chuyên khoa, tìm bệnh nhân, tiếp nhận, NoShow, hủy |
| Doctor | Dashboard, hồ sơ, mật khẩu, lịch hẹn, ca làm việc, chi tiết lịch thuộc mình |
| Admin Web | Dashboard, hồ sơ, mật khẩu và danh mục chỉ đọc |
| Admin Desktop | Đăng nhập; shell tổng quan/tài khoản/bác sĩ/chuyên khoa/ca/audit; tạo/sửa tài khoản, phân quyền, sửa chuyên khoa; tạo/sửa/chọn bác sĩ; tạo/sửa/chi tiết ca, đóng slot; hồ sơ, mật khẩu, chi tiết audit và xác nhận |

48 màn hình Web được kiểm tra không tràn toàn trang ở độ rộng **1440, 768, 320, 360 px**; bảng rộng cuộn trong vùng riêng. Ảnh lưu ở 1440 và 360 px. Axe không báo violation với các tag WCAG 2 A/AA và 2.1 AA được chạy trên các trang ở 1440 và menu mobile. Đây là phạm vi kiểm tra tự động, không phải chứng nhận toàn bộ accessibility.

22 màn hình/form WinForms được chụp kích thước mặc định và MinimumSize, **DeviceDpi=120**. Form dài có vùng cuộn, nút Lưu/Hủy vẫn hiển thị; bảng co cột/ellipsis và cuộn trong vùng dữ liệu. Form chạy thật bằng native message loop nhưng ẩn khỏi desktop người dùng; đã review ảnh, chưa thực hiện thao tác thủ công trên màn hình vật lý hoặc chuyển giữa nhiều DPI.

## 4. Chạy lại

Trên Windows có SQL Express, runtime/SDK, Node/npm, Python và sqlcmd:

```powershell
dotnet restore HospitalAppointmentSystem.sln
Push-Location tests/browser
npm ci
npx playwright install chromium
Pop-Location
powershell -NoProfile -File scripts/verify-phase7.ps1 -FinalQa
powershell -NoProfile -File scripts/verify-demo.ps1
```

Runner tạo database riêng, chạy .NET với `HOSPITAL_TEST_SQLSERVER`, khởi động API/Web ẩn, giữ ảnh/log và dọn process/database trong finally. Mặc định SQL `.\SQLEXPRESS`, cổng QA 5034/5175, kiểm tra demo 5038/5178; chọn cổng khác bằng `-ApiPort`/`-WebPort` khi bận. Không dừng dịch vụ hiện có để chiếm cổng.

Chạy UI tích hợp trước 22:30 giờ Việt Nam để còn khung hôm nay cho tiếp nhận. Nhóm .NET dùng clock test nên không bị giới hạn này. Runner chờ 60 giây giữa các nhóm đăng nhập để tuân thủ rate limiter. Các switch `-SkipBuild`, `-SkipDotNet`, `-SkipRegression` chỉ phục vụ kiểm tra từng phần; nghiệm thu đầy đủ phải chạy lệnh không có switch Skip. `-FinalQa` cần hồi quy được bật để chạy gallery.

## 5. Bằng chứng cục bộ

Artifact nằm trong thư mục bị git ignore; clone mới chạy lại runner để sinh. Không commit binary/screenshot/log vào source.

| Bằng chứng | Đường dẫn tại lần nghiệm thu (thư mục `codes` cũ) |
|---|---|
| Lượt đầy đủ 18/10 | `tests/browser/artifacts/phase8/0d3b172b642e41a2950f5e750fd82157/` |
| Lượt UI sau sửa 19/10 | `tests/browser/artifacts/phase8/f409ba2462ce475b8462a861310d0555/` |
| Build / .NET / SQL | `build.log`, `dotnet-tests.log` (lượt đầy đủ), `sql-evidence.log`, `cleanup.log` trong thư mục run |
| Các nhóm Playwright | `phase7-browser.log`, `browser-regression.log`, `final-ui.log` |
| Gallery Web | `<run>/final-ui/index.html`, `pages.json`, 96 ảnh trang, ảnh menu/modal và `receipt.pdf` |
| Gallery Desktop | `<run>/final-desktop/screens.json`, 44 ảnh; log ở `<run>/final-ui/desktop.log` |
| Desktop hồi quy | `<run>/desktop-regression.log` |
| TRX .NET | `tests/*/TestResults/phase7.trx` |
| Demo lifecycle | `tmp/phase8-demo-final.log`, log từng hành động trong `tmp/phase8-demo/c8c9f95bb7e04696a52ea3204e7304fe/` |

## 6. Kết luận phạm vi bàn giao

Phase 1–8 hoàn tất cho đồ án/demo cục bộ: API, Web, WinForms và SQL dùng chung dữ liệu; 28 UC/BR có truy vết và kiểm thử, các luồng demo chính đã chạy xuyên suốt. Xem [ma trận UC](usecase-coverage.md), [demo](demo-guide.md), [kiến trúc](architecture.md), [database](database-design.md), [giải thích kỹ thuật](technical-explanation.md).

Giới hạn: SQL Express/Windows Authentication là môi trường đã chạy; Docker được hướng dẫn từ tài liệu Microsoft nhưng chưa thực thi. Đã test restart tiến trình giữ dữ liệu, chưa tắt/bật máy vật lý. Chromium là browser nghiệm thu; chưa test Firefox/Safari, tải lớn, đa máy hoặc WinForms đa DPI thủ công. Không bao gồm triển khai production, bệnh án, chẩn đoán, kê đơn, thanh toán hay gửi SMS/email.
