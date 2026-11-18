# **PHÂN TÍCH THIẾT KẾ HƯỚNG ĐỐI TƯỢNG**

## **Topic: Medical appointment scheduling system**

---

_**Sai Gon University, Ho Chi Minh City, VietNam**_\
_**[2026–2027] CSharp**_\
Class name: **DCT124C3**\
Professor: **Đỗ Như Tài**
|Student|Student ID|
|------|-----|
| Lê Nguyễn Anh Bảo | 3124411026 |
| Trần Tiến Phát | 3124411222 |
| Nguyễn Khải Hoàn Ca | 3124411044 |

---

## Chạy demo bằng Docker Compose

Tại thư mục gốc repository, mở Docker Desktop ở chế độ Linux containers:

```powershell
Copy-Item .env.example .env
docker compose up -d --build --wait --wait-timeout 360
```

Chỉ copy `.env` ở lần đầu; giữ file hiện có khi chạy lại. Web: http://localhost:5174;
Swagger: http://localhost:5014/swagger. Compose chạy SQL Server, API và Web,
tự migration và seed dữ liệu mẫu khi database mới. Dữ liệu được giữ trong volume.
WinForms Admin chạy trên Windows và kết nối API ở cổng 5014.

Xem [hướng dẫn Docker Compose](codes/docs/instructions/docker-compose-demo.md)
và [kế hoạch hoàn thiện demo OOAD](OOAD_DEMO_PLAN.md).
