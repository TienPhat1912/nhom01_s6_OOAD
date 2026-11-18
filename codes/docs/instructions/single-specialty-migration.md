# Chuyển bác sĩ về một chuyên khoa

Đợt A2 dùng `Doctors.SpecialtyId` bắt buộc và FK Restrict tới `Specialties`, thay bảng liên kết nhiều–nhiều. Lịch hẹn vẫn giữ chuyên khoa đã ghi, kể cả sau khi bác sĩ chuyển chuyên khoa. Không sửa migration cũ; migration mới là `20261118014141_SingleDoctorSpecialty`.

## Database demo mới

Ưu tiên database riêng cho OOAD. Chuỗi migrations tạo schema hiện hành rồi seed mỗi bác sĩ với một chuyên khoa. API khởi động tự migration theo cấu hình demo/Compose đã có.

## Database có dữ liệu cũ

Trước khi nâng cấp, sao lưu bằng quy trình của nhóm và xem truy vấn sau trên database cần chuyển:

```sql
SELECT d.Id, d.DoctorCode, d.FullName,
       COUNT(ds.SpecialtyId) AS SpecialtyCount
FROM Doctors d
LEFT JOIN DoctorSpecialties ds ON ds.DoctorId = d.Id
GROUP BY d.Id, d.DoctorCode, d.FullName
HAVING COUNT(ds.SpecialtyId) <> 1;
```

Nếu có bác sĩ chưa có hoặc có nhiều chuyên khoa, migration từ chối với mã SQL 51001 và tiền tố `OOAD_A2`. Không tự chọn chuyên khoa chính/đầu tiên. Nhóm cần xác định một chuyên khoa hiện hành cho từng bác sĩ và xử lý các lịch tương lai bị ảnh hưởng trước khi điều chỉnh liên kết cũ. Không thay chuyên khoa trên lịch hẹn đã lưu để làm cho truy vấn hết lỗi.

Khi mọi bác sĩ có đúng một liên kết, migration chép giá trị đó sang `Doctors.SpecialtyId`, đặt NOT NULL/FK rồi bỏ bảng liên kết. Chạy trong transaction migration; nếu bị từ chối thì dữ liệu và phiên bản migration giữ nguyên. Cột `IsPrimary` cũ không còn ý nghĩa khi chỉ có một chuyên khoa.

Từ thư mục `codes`, đặt connection string đúng database riêng cần nâng cấp rồi chạy:

```powershell
dotnet ef database update --project src/Hospital.Infrastructure --startup-project src/Hospital.Api
```

Migration Down tái tạo một liên kết `IsPrimary = true` từ chuyên khoa hiện hành của mỗi bác sĩ; nó không khôi phục những liên kết đã được nhóm chủ động gỡ trước khi nâng cấp. Sao lưu là nguồn khôi phục dữ liệu nhiều chuyên khoa ban đầu.

## Hợp đồng API và ứng dụng

- `SaveDoctorRequest` dùng một số `SpecialtyId` dương, không còn `SpecialtyIds`/`PrimarySpecialtyId`.
- `DoctorDto` trả `SpecialtyId` và `SpecialtyName`; `DoctorDetailDto` trả `SpecialtyId` và đối tượng `Specialty`.
- Hồ sơ bác sĩ trong `UserProfileDto` trả `SpecialtyName`.
- Form Admin dùng một ComboBox. Phải build/cập nhật API, Web và Desktop cùng phiên bản. Client cũ gửi danh sách sẽ bị từ chối do thiếu `SpecialtyId` hợp lệ.
- Đổi chuyên khoa hiện hành kiểm tra lịch Confirmed tương lai bị ảnh hưởng; cần Staff xử lý trước. Đặt mới kiểm tra chuyên khoa gửi lên đúng chuyên khoa hiện hành đang hoạt động.

Đợt này chưa thay lịch 30 phút, chưa thêm quy tắc lặp tuần, sự kiện hay lịch nhắc mới. Các việc đó thuộc A3–A8/B trong kế hoạch.
