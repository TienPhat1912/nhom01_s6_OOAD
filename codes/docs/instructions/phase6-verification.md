# Kiểm chứng Phase 6 — 18/10/2026

Phạm vi: Windows Forms Admin → HTTP API → SQL Server, dùng chung backend với Web. Phase 7 tiếp tục nghiệm thu chuỗi tích hợp đầy đủ giữa các giao diện; Phase 8 tiếp tục hoàn thiện và bàn giao.

## Chức năng đã triển khai

| Nhóm | Giao diện và hành vi |
|---|---|
| Tổng quan | Số tài khoản/bác sĩ/chuyên khoa thật; ca làm việc 7 ngày; tìm kiếm, phân trang, trạng thái kết nối và đồng hồ UTC+7 |
| Tài khoản | Cấp Staff/Doctor/Admin; sửa thông tin, khóa/mở khóa; phân quyền có xác nhận; bảo vệ Admin cuối; Patient giữ vai trò gắn hồ sơ |
| Bác sĩ | Thêm/sửa tên, chức danh, giới thiệu, đường dẫn ảnh, chuyên khoa và chuyên khoa chính; ngừng hoạt động giữ lịch sử; tên đồng bộ với tài khoản liên kết |
| Chuyên khoa | Thêm/sửa mã, tên, mô tả, trạng thái; API kiểm tra duy nhất và lịch hẹn bị ảnh hưởng |
| Lịch làm việc | Lọc khoảng ngày/bác sĩ, mặc định 7 ngày; tạo/sửa ca, xem số khung 30 phút dự kiến; xem khung hiện hành và lịch sử; đóng khung trống với lý do |
| Nhật ký | Phân trang/lọc loại đối tượng; thời gian, người thực hiện, thao tác; chi tiết trước/sau chỉ đọc |
| Cá nhân | Sửa hồ sơ Admin, đổi mật khẩu, đăng xuất; đóng phiên khi quyền bị thu hồi, tự đổi quyền/khóa hoặc đổi mật khẩu |

Ứng dụng Desktop chỉ gọi `HospitalApiClient`; không mở kết nối SQL trực tiếp. Token nằm trong `AdminSession`, không lưu ra file. Mọi thao tác lưu sử dụng API hiện có, ngoại trừ bổ sung endpoint đọc chi tiết tài khoản và metadata phục vụ lựa chọn hồ sơ. Không phát sinh migration hoặc xóa cứng dữ liệu lịch sử.

`GET /api/admin/users/{id}` đọc lại thông tin trước khi sửa. Danh sách bác sĩ bổ sung `linkedUserId` chỉ có giá trị với Admin; bộ chọn không cho gán hồ sơ đã liên kết tài khoản khác. Chi tiết bác sĩ bổ sung `primarySpecialtyId` để giữ chuyên khoa chính khi sửa. Nhật ký trả `AuditLogDto` gồm tên người thực hiện. Backend kiểm tra lại liên kết, vai trò và lịch hẹn tại lúc ghi; trạng thái nút trên Desktop chỉ hỗ trợ thao tác.

## Form và xử lý lỗi

- Dùng chung tokens, button/grid/banner; `EditorForm` cung cấp validation dưới ô nhập, vùng cuộn, xác nhận và trạng thái lưu. Form bác sĩ có danh sách chọn nhiều chuyên khoa, chuyên khoa chính lấy từ các mục đã chọn.
- Hộp xác nhận dùng nút Hủy làm mặc định; không lưu khi hủy. Trong khi gửi yêu cầu, vô hiệu nút/field và chặn đóng form để không bỏ lửng thao tác ghi.
- Lỗi API giữ nội dung form, thông báo và mã hỗ trợ. Khi nhận 5xx hoặc mất kết nối lúc lưu, vô hiệu nút Lưu và hướng dẫn đóng form/tải lại danh sách để kiểm tra kết quả; không tự gửi lại yêu cầu quản trị chưa có idempotency key.
- Đọc danh sách thất bại xóa dữ liệu cũ và cho phép Làm mới. Khi nhận 401/403, kết thúc phiên Admin và trở về đăng nhập với thông báo.
- Lịch làm việc dùng ngày/giờ Việt Nam, khoảng lọc tối đa 366 ngày. Chặn ca quá khứ, khoảng giờ không chia hết 30 phút; API chặn chồng lấn và ca còn Confirmed/CheckedIn. Slot cũ sau đổi ca được giữ ở trạng thái lịch sử.
- Đóng slot chỉ khả dụng với khung trống trong tương lai; API kiểm tra lại để xử lý trường hợp người khác vừa đặt. Không có chức năng mở lại slot cũ ngoài hợp đồng API.

## Kết quả nghiệm thu

| Kiểm tra | Kết quả |
|---|---|
| Build solution với `--no-incremental --disable-build-servers -m:1` | 0 warning, 0 error |
| .NET toàn solution | 82 passed, 0 failed, 0 skipped: 8 unit + 68 integration + 6 Web; gồm 8 SQL Server tests |
| API mới | 3 tests: chi tiết tài khoản/quyền/không trả mật khẩu; liên kết bác sĩ chỉ cho Admin và chuyên khoa chính; nhật ký/tên người thực hiện/bộ lọc |
| WinForms smoke | 87 assertions đạt trên message loop Windows thật và API/SQL Server |
| Tokens | `generate_design_tokens.py --check` đồng bộ |
| Hình ảnh | Đã xem dashboard, form bác sĩ, trạng thái mất kết nối; kiểm tra kích thước chuẩn/tối thiểu và nút Lưu không bị cắt |

Smoke chạy `Application.Run`, bật kiểm tra cross-thread, mở form với opacity=0 và chụp bằng `DrawToBitmap`. Kiểm tra điều hướng shell → form → lưu → tải lại; tạo/sửa chuyên khoa, bác sĩ và tài khoản; chọn đúng hồ sơ; phân quyền/khóa/mở khóa; bảo vệ Admin cuối; tạo/sửa ca và giữ slot cũ; đóng khung có lý do; chặn ca/khung/bác sĩ/chuyên khoa đang có lịch hẹn; hủy lịch rồi ngừng hoạt động thành công; danh mục Patient phản ánh thay đổi Desktop qua API chung; nhật ký, hồ sơ và đổi mật khẩu/đăng nhập lại.

Riêng kiểm tra mất kết nối dùng `HttpMessageHandler` giả để chủ động phát lỗi vận chuyển; các luồng nghiệp vụ còn lại ghi/đọc qua API thật với database `HospitalPhase6Ui_20261018`. Hộp xác nhận được kiểm tra mặc định Hủy; smoke các luồng ghi tiêm `IDesktopDialogs` để tự xác nhận/hủy, tránh hộp thoại cần người bấm. Form chọn bác sĩ và điều hướng shell vẫn chạy modal thật.

Đã sửa lỗi nút Lưu bị cắt do vùng footer thiếu chiều cao, thông báo nhiều dòng bị rút gọn và tên Admin ở sidebar bị ghép dòng. Một test metadata ban đầu kỳ vọng Doctor được đọc danh mục; đã sửa test theo phân quyền hiện có (Doctor nhận 403), không mở rộng quyền API.

## Chạy lại

Từ thư mục `codes`, dừng app trước khi build:

```powershell
dotnet restore HospitalAppointmentSystem.sln -m:1
dotnet build HospitalAppointmentSystem.sln --no-restore --no-incremental --disable-build-servers -m:1
$env:HOSPITAL_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet test HospitalAppointmentSystem.sln --no-build --no-restore -m:1 --logger 'trx;LogFileName=phase6.trx'
python scripts/generate_design_tokens.py --check
```

Không đặt biến SQL Server sẽ bỏ qua 8 tests. Quyền Windows cần cho phép tạo/xóa database `HospitalPhase3Tests_<GUID>`. Nếu sandbox không có thông tin xác thực Windows, chạy kiểm thử SQL ngoài sandbox.

Terminal API, tạo database **mới riêng cho mỗi lượt**:

```powershell
$phase6Database = 'HospitalPhase6Ui_' + [guid]::NewGuid().ToString('N')
$env:ConnectionStrings__DefaultConnection = "Server=.\SQLEXPRESS;Database=$phase6Database;Trusted_Connection=True;TrustServerCertificate=True;"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Database__SeedDemo = 'true'
dotnet run --project src/Hospital.Api --no-build --no-launch-profile --urls http://localhost:5034
```

Terminal smoke, từ `codes` trên Windows có Windows Desktop Runtime 9:

```powershell
$env:HOSPITAL_API_URL = 'http://localhost:5034/'
dotnet run --project tests/Hospital.DesktopSmoke --no-build
```

Smoke tạo dữ liệu và đổi mật khẩu Admin demo của database thử nghiệm. Không trỏ vào HospitalDb hoặc database sử dụng thật; không dùng lại database đã chạy smoke thành công. Không vô hiệu rate limiter khi kiểm thử. `dotnet test` không tự chạy DesktopSmoke; cần chạy lệnh riêng ở trên. Build lại source trước `--no-build`, đặc biệt vì repository còn theo dõi một số `bin/obj` cũ.

Ảnh nằm trong `tests/browser/artifacts/phase6` đã gitignore. Sau nghiệm thu dừng API và xóa đúng database riêng đã tạo; không xóa database dùng chung. Không đưa output build hay dữ liệu thử vào commit.

## Giới hạn

Đã chạy WinForms thật ở chế độ ẩn và xem ảnh render, chưa nghiệm thu GUI thủ công toàn diện, screen reader, high contrast hoặc nhiều DPI/màn hình. Không chạy lại Chromium trong Phase 6 vì không sửa giao diện Web; kiểm thử MVC/API vẫn nằm trong 82 tests hồi quy. Chuỗi tương tác Web–Desktop đầy đủ, cạnh tranh từ giao diện và 18 kịch bản tích hợp tiếp tục ở Phase 7. Các bản ghi quản trị chưa có RowVersion; quy tắc nghiệp vụ kiểm tra trong transaction nhưng hai Admin sửa cùng hồ sơ có thể ghi đè theo lần lưu cuối, đúng hợp đồng backend hiện tại.

Sau nghiệm thu đã dừng API thử nghiệm và xóa đúng database `HospitalPhase6Ui_20261018`; truy vấn xác nhận còn 0 database trùng tên. HospitalDb vẫn có 6 tài khoản và 2 lịch hẹn hiện có.
