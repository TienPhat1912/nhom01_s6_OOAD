# Kiểm chứng Phase 5 — 18/10/2026

Phạm vi: Patient Web, Staff Portal và Doctor Portal qua MVC → HTTP API → SQL Server. Phase 6 (Desktop CRUD) và Phase 7 (tích hợp đầy đủ Web/Desktop) vẫn là công việc tiếp theo.

## Chức năng đã triển khai

| Nhóm | Giao diện và hành vi |
|---|---|
| Tài khoản | `/Account/Register` tạo đúng tài khoản Patient và đăng nhập; `/Account/ChangePassword` kiểm tra mật khẩu cũ/xác nhận, đăng xuất sau khi đổi, backend thu hồi phiên cũ |
| Đặt / đổi lịch | `/Booking/Start` → chuyên khoa → bác sĩ → ngày còn lịch trống → giờ trống → xem lại → xác nhận → `/Booking/Receipt/{id}`; hỗ trợ in phiếu bằng trình duyệt |
| Nhân viên | `/Patients` tìm hồ sơ đang hoạt động theo mã/tên/điện thoại; chọn đúng bệnh nhân để đặt hộ; đối chiếu tên/ngày sinh/điện thoại trước khi chọn |
| Hủy / tiếp nhận / vắng | `/Reception/Cancel/{id}`, `/Reception/CheckIn/{id}`, `/Reception/NoShow/{id}` có phiên bản dữ liệu, lý do, xác nhận; tiếp nhận bắt buộc đánh dấu đã đối chiếu bệnh nhân |
| Tra cứu | Chuyên khoa liên kết bộ lọc bác sĩ; bác sĩ liên kết đặt lịch và danh sách lịch hẹn; lịch hẹn có ngày bắt đầu/kết thúc, trạng thái, từ khóa, thứ tự, liên kết hôm nay/sắp tới/lịch sử và phân trang |
| Bác sĩ | Dashboard, lịch tuần, danh sách có bộ lọc ngày, chi tiết, hồ sơ và đổi mật khẩu; không có thao tác đặt/hủy/tiếp nhận |

Giữ quy tắc backend: Patient chỉ đổi/hủy Confirmed khi còn ít nhất 2 giờ; Staff trước giờ bắt đầu và có lý do; tiếp nhận trong ngày hẹn và trước khi hết khung giờ; NoShow chỉ sau giờ kết thúc. `CheckedIn` hiển thị **Đã tiếp nhận**, không được diễn giải là hoàn tất khám. Các nút disabled chỉ hướng dẫn người dùng; API kiểm tra lại mọi yêu cầu POST.

Landing có CTA đặt lịch, đăng ký và tìm bác sĩ/chuyên khoa. Danh mục vẫn yêu cầu đăng nhập theo quyền API hiện có; không mở thêm dữ liệu công khai hoặc tạo nội dung bác sĩ giả. Ngày khám lấy từ `AvailableDates`, giờ lấy từ API khả dụng; việc xem chưa giữ chỗ. Avatar tiếp tục dùng chữ cái tên khi chưa có luồng ảnh được backend hỗ trợ.

## Nhất quán và xử lý lỗi

- Bổ sung `GET /api/patients/{id}` chỉ cho Staff, chỉ trả hồ sơ Patient đang hoạt động; không bổ sung quyền cấp tài khoản cho Staff.
- Thông tin chọn lịch và bản xác nhận được mã hóa/ký bằng ASP.NET Data Protection, có thời hạn 30 phút và gắn với UserId. Form xác nhận chỉ nhận bản đã bảo vệ; không tin thông tin bệnh nhân/bác sĩ/giờ hoặc vai trò do trình duyệt gửi lại.
- `RequestKey` được tạo tại bước xem lại và giữ nguyên khi thử lại cùng yêu cầu. Không kiểm tra slot lại trước khi gửi lại bản xác nhận: lần đầu có thể đã lưu nhưng mất phản hồi, cần backend trả lại cùng lịch hẹn.
- Khi đổi lịch, giữ RowVersion từ lúc bắt đầu chọn lịch. Khi hủy/tiếp nhận/vắng, giữ phiên bản ở form xác nhận. Không tự thay phiên bản cũ bằng phiên bản mới sau lỗi 409; người dùng tải lại rõ ràng trước khi xác nhận lại.
- Sau lỗi 5xx ở bước xác nhận, hướng dẫn thử lại cùng bản hoặc tra cứu danh sách. Đổi lịch gửi lặp có thể trả 409 theo hợp đồng API; cần xem kết quả hiện có. Bản chọn hết hạn/bị sửa/sai người dùng trả 400; lỗi backend giữ thông báo và mã hỗ trợ.
- POST có antiforgery; JWT chỉ nằm trong cookie HttpOnly đã bảo vệ. Mật khẩu không được phản chiếu vào HTML sau lỗi. Bệnh nhân/bác sĩ vẫn bị giới hạn phạm vi dữ liệu tại API.

## Kết quả nghiệm thu

| Kiểm tra | Kết quả |
|---|---|
| Build solution, `--no-incremental -m:1 --disable-build-servers` | 0 warning, 0 error |
| .NET toàn solution | 79 passed, 0 failed, 0 skipped; gồm 8 SQL Server tests |
| Nhóm Web → API mới | 8 tests: đăng ký/đổi mật khẩu; thử lại sau mất phản hồi SQLite/SQL Server; phiên bản cũ; Staff/lý do/tiếp nhận/vắng; slot bị chiếm; hạn 2 giờ/NoShow sớm; quyền và bộ lọc |
| Playwright Chromium | 11 passed: 8 hồi quy Phase 4 + 3 kịch bản Phase 5 |
| Responsive | 320, 360, 430, 768, 1440px; không tràn ngang toàn trang ở các màn hình kiểm tra |
| Axe | Không có violation thuộc WCAG 2 A/AA, 2.1 AA trên các màn hình được kiểm tra |
| Tokens | `generate_design_tokens.py --check` đồng bộ |

`WebWorkflowTests` chạy hai host MVC và API thật, sử dụng SQLite cô lập hoặc SQL Server thật. Chỉ lớp vận chuyển của test mất phản hồi có chủ ý thay response sau khi API đã commit; lần gửi lại phải trả cùng Id, database chỉ có một lịch. Test đổi/hủy cũ xác minh dữ liệu hiện tại còn nguyên; test slot bị người khác chiếm không tạo thêm lịch. Đồng hồ test điều khiển được để luôn kiểm chứng tiếp nhận và NoShow đúng mốc thời gian.

Browser dùng database riêng `HospitalPhase5Ui_20261018`, đăng ký bệnh nhân mới rồi đặt/đổi/hủy/đọc thông báo/đổi mật khẩu/đăng nhập lại. Staff tìm đúng hồ sơ, đặt hộ, nhập lý do hủy, đặt lịch hôm nay và đối chiếu để tiếp nhận. Doctor xem đúng lịch đã tiếp nhận, không được vào Booking/Patients. Đã kiểm tra bản in phiếu và xem ảnh desktop/mobile của form chọn giờ. Lỗi selector/hoa-thường trong test đã được chỉnh theo badge và nội dung thực tế. Hồi quy axe phát hiện liên kết đăng ký trong đoạn văn chưa có dấu hiệu ngoài màu sắc; đã thêm gạch chân cho liên kết nội dung và chạy lại đủ 11 browser tests thành công.

## Chạy lại

Từ thư mục `codes`, dừng các app trước khi build:

```powershell
dotnet restore HospitalAppointmentSystem.sln -m:1
dotnet build HospitalAppointmentSystem.sln --no-restore --no-incremental --disable-build-servers -m:1
$env:HOSPITAL_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet test HospitalAppointmentSystem.sln --no-build --no-restore -m:1 --logger 'trx;LogFileName=phase5.trx'
python scripts/generate_design_tokens.py --check
```

Không đặt biến SQL Server sẽ bỏ qua 8 test SQL. Tài khoản Windows cần quyền tạo/xóa database thử nghiệm `HospitalPhase3Tests_<GUID>`. Khi sandbox không cung cấp thông tin xác thực Windows, chạy các test SQL ngoài sandbox; không sửa authentication hay bỏ kiểm tra SQL. Build dùng một worker vì build mặc định trong sandbox máy kiểm chứng bị treo; build một worker hoàn tất bình thường.

Terminal API, tạo database **riêng**, không dùng HospitalDb:

```powershell
$phase5Database = 'HospitalPhase5Ui_' + [guid]::NewGuid().ToString('N')
$env:ConnectionStrings__DefaultConnection = "Server=.\SQLEXPRESS;Database=$phase5Database;Trusted_Connection=True;TrustServerCertificate=True;"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Database__SeedDemo = 'true'
dotnet run --project src/Hospital.Api --no-build --no-launch-profile --urls http://localhost:5034
```

Terminal Web:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Api__BaseUrl = 'http://localhost:5034/'
dotnet run --project src/Hospital.Web --no-build --no-launch-profile --urls http://localhost:5175
```

Terminal browser:

```powershell
cd tests/browser
npm ci
npx playwright install chromium
$env:HOSPITAL_WEB_URL = 'http://localhost:5175'
npm test
```

Có thể đặt `PLAYWRIGHT_CHROMIUM_PATH` đến Chromium đã cài. Chromium cần quyền tạo process; sandbox máy kiểm chứng trả EPERM nên kiểm thử browser chạy ngoài sandbox, headless. Không vô hiệu rate limiter: đợi hết cửa sổ 60 giây nếu chạy lại nhiều lần đăng nhập. Browser ưu tiên ca hôm nay để kiểm tra tiếp nhận; nếu mọi ca seed hôm nay đã hết, kiểm tra nút tiếp nhận lịch ngày sau bị khóa. Nhóm integration có đồng hồ cố định luôn chạy nhánh tiếp nhận/NoShow thành công. Dùng database mới khi chạy lại sau ngày seed hoặc nhiều lần đã chiếm hết slot hôm nay.

Ảnh/trace/report nằm trong thư mục đã gitignore dưới `tests/browser`. Tracked `bin/obj` cũ không thuộc commit Phase 5; luôn build source trước `--no-build`. Sau nghiệm thu, dừng API/Web thử nghiệm và xóa riêng database đã tạo cho lượt kiểm chứng. HospitalDb không được dùng cho các thao tác test.

## Giới hạn

Chưa kiểm chứng Safari/Firefox, screen reader hoặc tất cả mức zoom; axe không chứng minh WCAG đầy đủ. Không có thay đổi Desktop trong Phase 5; smoke WinForms và nghiệm thu CRUD Desktop thuộc Phase 6–7. Luồng browser không giả lập thời gian để thử NoShow; nghiệp vụ này được kiểm chứng xuyên MVC/API/database bằng đồng hồ test. Không phát sinh migration mới.

Sau nghiệm thu đã dừng API/Web thử nghiệm và xóa đúng database `HospitalPhase5Ui_20261018`; truy vấn kiểm tra xác nhận không còn database này.
