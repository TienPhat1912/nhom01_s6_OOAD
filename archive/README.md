# File lưu trữ cục bộ

Các file không cần để clone/build/chạy demo đã được gom vào thư mục này, giữ nguyên nội dung:

- `build/`: bản build, cache `bin/`, `obj/` và kết quả .NET test cũ.
- `previous-runs/`: log, PID, script tạm, ảnh và báo cáo browser của các lần chạy trước.
- `README.before-cleanup.md`: bản README trước khi chuẩn hóa hướng dẫn.
- `removed-codes/`: cấu hình demo cũ, thiết lập IDE, log và kết quả kiểm thử được giữ trước khi xóa thư mục `codes` ngày 10/11/2026. Cấu hình này chỉ để đối chiếu; cấu hình đang dùng nằm trong `codes/`.

Xem [danh mục di chuyển](MANIFEST.md). Git chỉ theo dõi README và danh mục này; dữ liệu lưu trữ không được commit. Khi clone mới, chạy setup/build/test để sinh lại các file cần thiết.

Thư mục `bin/`, `obj/`, `tmp/`, `TestResults/` và kết quả browser có thể xuất hiện trở lại sau khi chạy công cụ, nhưng đã được ignore và untrack. Mã nguồn, migrations, lockfile, design tokens và thư viện tĩnh trong `wwwroot/lib/` vẫn được Git theo dõi.

Báo cáo Markdown trong [`codes/docs/instructions/`](../codes/docs/instructions/) là lịch sử nghiệm thu/giải thích thiết kế của đồ án; các đường dẫn log/ảnh trong đó mô tả lần chạy cũ, không phải kết quả của máy vừa clone. Tài liệu Word/PDF được giữ trong [`codes/docs/`](../codes/docs/).
