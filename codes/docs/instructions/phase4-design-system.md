# Phase 4 — Design system đã triển khai

Ngày 17/10/2026. Thương hiệu giao diện: **An Tâm**, nền trắng/slate, nhấn teal, nội dung tiếng Việt. Component thể hiện trạng thái từ API; số liệu dashboard không được nhập cứng. Các màn hình tra cứu và form hồ sơ là điểm tích hợp để phát triển Phase 5–6.

## Nguồn tokens và cách dùng lại

`design/tokens.json` là nguồn chung. Chạy `python scripts/generate_design_tokens.py` sau khi đổi tokens; commit cả nguồn và hai output `wwwroot/css/tokens.css`, `Design/DesignTokens.g.cs`. CI/local dùng `--check` để phát hiện output cũ. CSS thuần, không cần Tailwind/npm khi chạy sản phẩm; npm chỉ dùng kiểm thử trình duyệt.

Primary button dùng `#0d5e56`, hover `#0f3b38`; xanh `#0d9488` dành cho chi tiết trang trí. Màu trạng thái đậm hơn bản đề xuất Phase 1 để chữ nhỏ rõ trên nền nhạt. Trạng thái luôn có nhãn tiếng Việt, không chỉ khác màu. `CheckedIn` là **Đã tiếp nhận**, không phải hoàn tất khám.

Font Segoe UI/system; Web dùng px, WinForms dùng point cho chữ và DPI scaling. Khoảng cách theo bội số 4; radius 6/10/16/24px. Web có breakpoint 1200/900/600px, sidebar thu vào menu ở 900px. Bảng dữ liệu cuộn ngang trong vùng có nhãn và focus; trang không tràn ngang tại 320px. WinForms tối thiểu 800×590 cho Login, 960×650 cho Admin shell.

## Component

| Nhu cầu | Web | WinForms |
|---|---|---|
| Theme | tokens.css + site.css | DesignTokens + UiTheme |
| Nút/chờ xử lý | `.btn`, `.spinner`, `data-busy` | UiButton, disabled + nhãn đang tải |
| Form | `.field`, DataAnnotations, validation summary | UiField + SetError, nhãn truy cập |
| Phản hồi | `_Message`, `_EmptyState`, `ApiError` | UiBanner, empty label trong vùng dữ liệu |
| Dữ liệu | `_AppointmentTable`, `_StatusBadge`, `_Pagination` | UiDataGrid, tìm kiếm + paging |
| Xác nhận | `_ConfirmDialog`, `data-confirm` trên form | MessageBox xác nhận đăng xuất |
| Điều hướng | `_Layout`, `_Navigation`, `_Brand` | AdminShellForm + sidebar |
| Icon | `<ui-icon name="calendar" />` SVG nội bộ | Nhãn/nút text với AccessibleName |

Form POST dùng Razor form tag helper để sinh antiforgery token. Thêm `data-confirm="Câu xác nhận cụ thể"` cho thao tác cần xác nhận. site.js dùng native `<dialog>`: focus ban đầu ở Hủy bỏ, Escape đóng, trả focus về nút gọi. Form chỉ chuyển trạng thái chờ sau validation và xác nhận; chặn submit lặp. API vẫn chịu trách nhiệm idempotency/concurrency nghiệp vụ ở phase tiếp theo.

Gallery `/Design` trình diễn button, form, validation, alert, empty, skeleton và stepper. Controller trả 404 ngoài Development. Dữ liệu minh họa trong gallery được ghi rõ, không được dùng làm dữ liệu nghiệp vụ. Skeleton thuộc gallery; trang MVC nghiệp vụ render ở server, thao tác submit dùng spinner và vùng `role=status`.

## Dữ liệu, phiên và lỗi

`Hospital.Client` dùng chung DTO Application; mỗi request gắn Bearer riêng, không dùng token global. Lỗi API được chuyển thành `HospitalApiException` gồm HTTP status, message, errors và trace. Kết nối lỗi/timeout/phản hồi không đọc được được ánh xạ 503/504/502.

Web gọi API ở server, JWT nằm trong cookie được bảo vệ bằng ASP.NET Data Protection, HttpOnly/SameSite=Lax; không xuất token ra HTML/localStorage. Expiration theo API, không sliding. Mỗi request có phiên gọi profile để phát hiện khóa/thu hồi token; 401/403 yêu cầu đăng nhập lại. Khi backend gián đoạn, giữ phiên nhưng màn hình nghiệp vụ trả lỗi thay vì số 0 hoặc dữ liệu cache. Các POST kiểm tra CSRF; redirect sau login chỉ chấp nhận URL nội bộ. Protected response có no-store. Production cần HTTPS, cấu hình API URL và lưu Data Protection keys phù hợp môi trường triển khai.

WinForms chỉ nhận Admin, token giữ trong bộ nhớ; backend tiếp tục kiểm tra quyền. Tải API bất đồng bộ, khóa thao tác trong lúc chờ, hủy request khi đóng form. Bảng chỉ đọc ở Phase 4. Không truy cập SQL trực tiếp từ Web/Desktop.

## Màn hình đang có

| Vai trò | Phạm vi |
|---|---|
| Công khai | Trang chủ, quyền riêng tư, đăng nhập |
| Patient | Dashboard, lịch hẹn và chi tiết/lịch sử, bác sĩ/chuyên khoa, thông báo/đánh dấu đã đọc, sửa hồ sơ |
| Staff | Dashboard/lịch hẹn, chi tiết, bác sĩ/chuyên khoa, sửa hồ sơ |
| Doctor | Dashboard/lịch hẹn, chi tiết, lịch làm việc 7 ngày, sửa liên hệ/chức danh/tiểu sử |
| Admin Web | Dashboard số liệu thật, tra cứu bác sĩ/chuyên khoa, sửa hồ sơ |
| Admin WinForms | Đăng nhập, tra cứu tài khoản/bác sĩ/chuyên khoa, tìm kiếm/phân trang |

Phase 5 tiếp tục đăng ký, đổi mật khẩu, chọn slot, đặt/đổi/hủy, Staff đặt hỗ trợ/tiếp nhận/no-show và mở rộng bộ lọc. Phase 6 tiếp tục form CRUD, phân quyền, lịch làm việc/đóng slot và audit. Chưa xác nhận toàn bộ 28 UC hoàn tất giao diện.

## Cơ sở thiết kế và giới hạn kiểm chứng

Semantic HTML, focus rõ, keyboard và tương phản tham khảo [Fluent 2 accessibility](https://fluent2.microsoft.design/accessibility). Native dialog áp dụng hành vi được mô tả trong [W3C H102](https://www.w3.org/WAI/WCAG22/Techniques/html/H102) và [ARIA dialog pattern](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/).

Kiểm thử axe trên các trang đã triển khai không phát hiện vi phạm bộ quy tắc WCAG 2 A/AA và 2.1 AA đã chọn; đây không phải chứng nhận WCAG toàn sản phẩm. Chưa kiểm thử screen reader, nhiều trình duyệt, Windows high contrast hoặc nhiều mức DPI. Xem [bằng chứng và lệnh chạy](phase4-verification.md).
