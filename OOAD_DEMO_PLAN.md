# Kế hoạch chỉnh sửa dự án để demo OOAD

Ngày rà soát: 18/11/2026.

Dự án có thể tái sử dụng để demo môn Phân tích thiết kế hướng đối tượng. Phần cần đầu tư nhiều nhất là quản lý lịch định kỳ và thống nhất mô hình dữ liệu với báo cáo lần 04. Giữ kiến trúc API, Web MVC, WinForms Admin và SQL Server hiện có; triển khai theo thứ tự mô hình dữ liệu, nghiệp vụ, giao diện, kiểm thử và diễn tập.

## 1. Căn cứ và phạm vi rà soát

- Báo cáo chính: `D:/learn_coding/current semester/OOAD/bao cao/S6_Nhom01_Lan04 (2).docx`.
- Đã trích đọc nội dung và xem các ảnh nhúng: 57 ảnh, gồm logo, sơ đồ use case, các phần sơ đồ tuần tự, sơ đồ lớp và mô hình quan hệ. Những sơ đồ lịch làm việc, kiểm tra điều kiện đặt, lớp và quan hệ được mở riêng để đối chiếu chi tiết.
- Mã nguồn: `codes/src`, các kiểm thử liên quan trong `codes/tests`, hướng dẫn và ma trận use case hiện có.
- Đây là rà soát tĩnh để lập kế hoạch. Chưa khởi động ứng dụng, chạy lại build/tests hoặc xác nhận giao diện thực tế trên máy. Các kết quả nghiệm thu Phase 3–8 trong repo là lịch sử, không chứng minh bản sửa theo báo cáo lần 04 đã đạt.
- Phạm vi đề xuất bám UC01–UC28, BR01–BR12 trong báo cáo. Các phát biểu yêu cầu trong báo cáo được dùng làm đối tượng đối chiếu với phần mềm.

## 2. Kết luận từ các hình trong báo cáo

| Nhóm hình | Nội dung cần thể hiện được khi demo | Kết quả đối chiếu |
|---|---|---|
| Use case tổng quát và các nhóm chi tiết ở Chương 3 | Bốn vai trò; Admin quản lý dữ liệu/lịch làm việc, Staff xử lý lịch hẹn, Doctor chỉ theo dõi lịch của mình | Kiến trúc và phân vai hiện tại phù hợp về cơ bản; cần rà quyền từng endpoint và màn hình |
| Use case đặt/đổi lịch | UC13 và UC17 dùng chung UC14; xem khung giờ không giữ chỗ | `AppointmentService.ValidateSlotAsync` đã được dùng chung khi ghi trong transaction |
| Sequence UC09–UC12 | Tạo định kỳ hoặc bổ sung một ngày; xem trước rồi xác nhận; sửa một ngày/định kỳ, gia hạn, đóng và mở lại | Code mới có tạo/sửa ca theo ngày và đóng một slot; thiếu phần lớn nhánh lịch định kỳ |
| Sequence UC13–UC20 | Xác định bệnh nhân, kiểm tra lại điều kiện, giữ mã lịch khi đổi, bảo toàn dữ liệu khi thất bại | Đã có phần lớn luồng và cơ chế transaction; cần kiểm chứng lại sau khi đổi mô hình lịch |
| Sequence UC21 | Xem thông báo thuộc mình; nội dung sự kiện cũ được giữ khi lịch đã đổi/hủy | Có danh sách và nội dung thông báo; cần hoàn thiện truy vết theo sự kiện |
| Hình 5.1–5.4 | 14 lớp dữ liệu, gồm quy tắc lịch, mẫu lặp tuần, ca thực tế, sự kiện và lịch nhắc | Chưa có đủ các khái niệm tương ứng; entity hiện chủ yếu lưu dữ liệu, nghiệp vụ nằm trong service |
| Hình 6.1 | Bác sĩ thuộc một chuyên khoa; ca có nguồn định kỳ tùy chọn; sự kiện liên kết khung trước/sau; nhắc và thông báo gắn sự kiện | Khác cấu trúc hiện tại, cần migration và cập nhật DTO/UI/tests |

Các hình tuần tự dùng Boundary–Control–Entity ở mức phân tích. Khi thuyết trình cần giải thích ánh xạ sang triển khai: View/Form là Boundary; Controller và Service cùng hiện thực trách nhiệm Control; entity và EF Core hiện thực dữ liệu và lưu trữ. Không cần tạo một class C# cho mọi lifeline của sơ đồ phân tích.

## 3. Ma trận hiện trạng theo nhóm use case

| Use case | Nền tảng hiện có | Việc cần làm |
|---|---|---|
| UC01–UC04 | Đăng ký, đăng nhập, hồ sơ, mật khẩu, vai trò và phiên | Rà dữ liệu bắt buộc, chuẩn hóa đăng nhập, cập nhật đồng thời và ánh xạ lớp tài khoản/bệnh nhân |
| UC05–UC08 | Tra cứu chuyên khoa, bác sĩ, chi tiết và slot | Chuyển sang một chuyên khoa/bác sĩ; lọc cả trạng thái ca thực tế và slot |
| UC09–UC12 | Xem/tạo/sửa ca theo ngày, đóng slot | Bổ sung lịch định kỳ, preview, gia hạn, ngoại lệ một ngày, đóng ca, mở lại ca/slot |
| UC13–UC14 | Đặt lịch và kiểm tra dùng chung; chống gửi lặp khi đặt | Giữ các bảo vệ sẵn có; cập nhật theo mô hình ca và slot 20 phút |
| UC15–UC18 | Xem/tìm/đổi/hủy; hạn 2 giờ; lý do Staff; lịch sử | Chuẩn hóa sự kiện, truy vết và hành vi thử lại khi chưa rõ kết quả |
| UC19–UC20 | Tiếp nhận, không đến, người và thời điểm xử lý | Sửa biên thời gian không đến và chạy lại kiểm thử cạnh tranh |
| UC21 | Thông báo trong ứng dụng, worker và outbox nhắc trước 1 giờ | Liên kết sự kiện, chống trùng theo sự kiện/loại và giữ nội dung lịch sử |
| UC22–UC28 | Admin cấp/sửa/phân quyền, bác sĩ/chuyên khoa, bảo vệ Admin cuối | Chuyên khoa đơn, bảo toàn liên kết lịch sử, kiểm tra ảnh hưởng và phát hiện cập nhật cũ |

## 4. Danh sách công việc ưu tiên

P0 là phần cần hoàn thành để demo bám mô hình báo cáo lần 04. P1 hoàn thiện truy vết, trình bày và nghiệm thu. P2 là phần củng cố sau khi các luồng chính ổn định.

### A. Thống nhất dữ liệu và cách giải thích thiết kế

- [ ] **A1 P0** Lập bảng ánh xạ đủ 14 lớp báo cáo sang entity, bảng, service, DTO và màn hình. `CaTheoNgay` có thể tiếp tục dùng tên `WorkingSchedule`; tên tiếng Anh hoặc khóa GUID không tự tạo thành lỗi nghiệp vụ, nhưng phải ghi rõ khác biệt thiết kế logic/triển khai.
- [ ] **A2 P0** Chuyển quan hệ bác sĩ–chuyên khoa sang một chuyên khoa hiện hành. Báo cáo dùng quan hệ 1–n, code dùng `DoctorSpecialty` nhiều–nhiều và chọn chuyên khoa chính. Cập nhật Domain, EF mapping, Catalog/Admin DTO, service, form bác sĩ, seed và tests. Với dữ liệu nhiều chuyên khoa, cần quy tắc chuyển đổi có kiểm tra lịch sử; ưu tiên database demo mới để tránh đổi nghĩa dữ liệu cũ.
- [ ] **A3 P0** Bổ sung `ScheduleRule` tương ứng `QuyTacLich`, `WeeklyPattern` tương ứng `MauLapTuan`; mở rộng ca theo ngày bằng nguồn quy tắc tùy chọn, mã ca, trạng thái mở và dấu đã chỉnh riêng.
- [ ] **A4 P0** Thiết kế ràng buộc duy nhất cho ca `(DoctorId, WorkDate, ShiftCode)` kể cả ca đóng; khung `(WorkingScheduleId, StartTime)`; kiểm tra nguồn quy tắc cùng bác sĩ và khung nằm trong ca. Rà lại cách giữ slot lịch sử để tránh tạo trùng khóa.
- [ ] **A5 P1** Hoàn thiện `AppointmentStatusHistory` thành sự kiện lịch hẹn có loại thao tác rõ ràng, trạng thái trước/sau, khung trước/sau, người và thời điểm. Hiện có ID khung trước/sau nhưng chưa khai báo quan hệ FK tương ứng trong `HospitalDbContext`.
- [ ] **A6 P1** Bổ sung lịch nhắc gắn với sự kiện đặt/đổi; thông báo tham chiếu sự kiện, unique theo `(EventId, NotificationType)`. Có thể tiếp tục dùng outbox làm cơ chế xử lý nền, nhưng cần phân biệt outbox với thực thể lịch nhắc trong báo cáo.
- [ ] **A7 P1** Bổ sung mã yêu cầu và kết quả cho thao tác cần chống gửi lặp, đặc biệt tạo/gia hạn lịch hàng loạt. Cùng mã và nội dung trả kết quả cũ; cùng mã khác nội dung bị từ chối. Hiện đặt lịch có `RequestKey`/fingerprint; tạo/sửa lịch làm việc chưa có.
- [ ] **A8 P1** Rà hành vi xóa và FK với dữ liệu lịch sử. Cấu hình hiện còn một số Cascade/SetNull; chọn Restrict hoặc cơ chế giữ tham chiếu phù hợp cho dữ liệu truy vết. Giao diện ngừng hoạt động không được biến thành xóa vật lý.

Vị trí chính: `Hospital.Domain/Entities`, `Hospital.Infrastructure/Data/HospitalDbContext.cs`, migrations và các DTO. Thực hiện migration trên database thử riêng, xác nhận dữ liệu lịch sử trước khi áp dụng cho database có dữ liệu cần giữ.

### B. Hoàn thiện lịch làm việc theo báo cáo

- [ ] **B1 P0** Dùng thống nhất ca sáng 07:00–11:00, ca chiều 13:00–17:00, 20 phút/khung, 12 khung/ca. Hiện service chia 30 phút; seed dùng 08:00–11:30 và 13:30–17:00. Sửa cả validation, preview, seed, fixture và mô tả UI. Giờ thực tế của ca được điều chỉnh vẫn phải thỏa các quy tắc bảo toàn lịch sử và thời lượng slot.
- [ ] **B2 P0** UC10 tạo lịch định kỳ: chọn bác sĩ, các thứ/ca, khoảng ngày hữu hạn tính cả hai biên; preview ngày/ca/số slot; xác nhận mới ghi toàn bộ. Bổ sung ca một ngày không tạo quy tắc lặp.
- [ ] **B3 P0** UC11 chỉnh một ngày: không đổi quy tắc nguồn; đánh dấu ca chỉnh riêng. Chỉ thay đổi các slot chưa bị tham chiếu và đáp ứng điều kiện thời gian.
- [ ] **B4 P0** UC11 sửa định kỳ từ ngày hiệu lực: giữ ca trước mốc, ca đóng, ca chỉnh riêng, ca bổ sung; tạo phiên bản quy tắc kế tiếp. Nếu thay toàn bộ quy tắc cũ, ngừng áp dụng và giữ khoảng ngày hợp lệ. Xung đột với ca được giữ phải từ chối toàn bộ.
- [ ] **B5 P0** UC11 gia hạn: chỉ thêm ca thiếu sau ngày kết thúc cũ; không tạo trùng, ghi đè hoặc mở lại ca đóng.
- [ ] **B6 P0** UC12 đóng một slot hoặc cả ca theo ngày; UC11 mở lại đúng ca/slot chưa bắt đầu. Mở lại giữ mã/thời gian/lịch sử và dấu chỉnh riêng; không phục hồi lịch hẹn đã hủy.
- [ ] **B7 P0** Preview chỉ đọc; khi xác nhận phải kiểm tra lại dữ liệu và quyền trong transaction. Có bất kỳ xung đột nào thì không lưu một phần. Trả chi tiết ngày/ca xung đột trong phạm vi quyền Admin.
- [ ] **B8 P0** UC09 và UC08 hiển thị ca thực tế, nguồn và trạng thái. Không suy ra slot ngoài phạm vi quy tắc; ca bổ sung ngoài thời hạn quy tắc vẫn được hiển thị nếu đã tồn tại và hợp lệ.

Vị trí chính: `ScheduleService.cs`, `IScheduleService`, DTO schedules, `SchedulesController`, `TimeSlotsController`, `Hospital.Client`, `ScheduleEditorForm`, `ScheduleDetailsForm`, `AdminShellForm`, Web `ScheduleController` và view lịch bác sĩ.

Nghiệm thu mẫu: Tạo thứ Hai/Tư/Sáu, ca sáng, trong hai tuần đầy đủ phải có 6 ca và 72 slot. Gửi lại cùng yêu cầu không tăng số bản ghi. Đóng một ca rồi sửa quy tắc phải giữ ca đóng; mở lại phải giữ nguyên ID. Hủy preview không phát sinh dữ liệu.

### C. Giữ đúng quy tắc lịch hẹn và quản trị

- [ ] **C1 P0** Giữ bốn trạng thái hiện có: Confirmed, Cancelled, CheckedIn, NoShow. Đặt hợp lệ được xác nhận ngay; Confirmed và CheckedIn chiếm chỗ. Không thêm bước duyệt hoặc hoàn tất khám ngoài phạm vi báo cáo.
- [ ] **C2 P0** Sửa biên UC20: báo cáo cho phép ghi Không đến khi `now >= end`; code hiện từ chối khi `end >= now`, nên chỉ cho phép sau mốc kết thúc. Kiểm tra cả lớp hiển thị nút để UI và API thống nhất.
- [ ] **C3 P0** Chạy lại các biên: Patient đổi/hủy khi còn đúng 2 giờ được phép, dưới 2 giờ bị chặn; Staff trước giờ bắt đầu và bắt buộc lý do; tiếp nhận đúng ngày, trước giờ kết thúc. Hai khoảng tiếp giáp không chồng lấn.
- [ ] **C4 P0** Giữ transaction Serializable, RowVersion và unique index có điều kiện của lịch hẹn. Sau thay đổi lịch làm việc, xác nhận hai người tranh cùng slot chỉ một người thành công, đổi thất bại giữ slot cũ.
- [ ] **C5 P1** Rà thay đổi bác sĩ/chuyên khoa, ngừng hoạt động và tài khoản: không gây ảnh hưởng lịch đã tiếp nhận hoặc viết lại lịch sử; lịch Confirmed bị ảnh hưởng cần Staff xử lý trước. Cần kiểm tra cả ca/quy tắc liên quan sau khi thêm lịch định kỳ.
- [ ] **C6 P1** Các form quản trị cập nhật từ dữ liệu cũ cần cơ chế phát hiện xung đột như nhánh sequence UC23/UC28. Transaction lúc lưu chưa đủ để phát hiện người khác sửa kể từ lúc mở form; bổ sung phiên bản/điều kiện so sánh và yêu cầu tải lại khi cần.
- [ ] **C7 P1** Rà quyền theo từng hành động. Admin có thể cần truy cập danh mục phục vụ quản trị; không vì thế được xử lý lịch hẹn thay Staff. API hiện có một số endpoint tra cứu cho Admin, cần phân biệt rõ với quyền nghiệp vụ lịch hẹn.

### D. Thể hiện được thiết kế hướng đối tượng

- [ ] **D1 P1** Tạo tài liệu truy vết `UC → sơ đồ tuần tự → Boundary/Control/Entity → phương thức code → bảng → test`. Cập nhật ma trận hiện có theo lần 04, nhất là UC09–UC12; trạng thái “hoàn tất” cũ chưa bao phủ lịch định kỳ mới.
- [ ] **D2 P1** Bổ sung các phương thức thay đổi trạng thái có ý nghĩa trên entity trọng tâm: đổi/hủy/tiếp nhận của lịch hẹn, đóng/mở ca/slot, gia hạn/ngừng quy tắc. Service tiếp tục kiểm tra quyền, truy vấn xung đột, điều phối nhiều đối tượng và transaction như Chương 5 mô tả. Tránh dồn mọi xử lý vào form hoặc controller.
- [ ] **D3 P1** Chuẩn bị ví dụ giải thích đóng gói, trách nhiệm lớp, quan hệ/bội số, interface và dependency injection từ code thật. Không diễn giải kế thừa actor “Người dùng” thành bắt buộc có kế thừa class Patient/Doctor/Staff/Admin; vai trò có thể được hiện thực bằng liên kết Role.
- [ ] **D4 P1** Ghi rõ khác biệt hợp lý như GUID, cột audit, RowVersion, outbox và dữ liệu phi chuẩn hóa phục vụ truy vấn. Khác biệt làm đổi nghiệp vụ như nhiều chuyên khoa hoặc thiếu quy tắc lịch phải được xử lý cụ thể.

### E. Dữ liệu và trải nghiệm demo

- [ ] **E1 P0** Bộ dữ liệu theo ngày demo: đủ bốn vai trò; nhiều bác sĩ để minh họa trùng lịch bệnh nhân; quy tắc định kỳ, ca bổ sung, ca chỉnh riêng, ca đóng; lịch được phép đổi, sát giờ, đã tiếp nhận, đã hủy, không đến và thông báo lịch sử.
- [ ] **E2 P0** Seed mới dùng chung quy tắc sinh lịch với nghiệp vụ, tránh một cách chia khung riêng. Không sửa giờ hoặc tái tạo các slot lịch sử đã có lịch tham chiếu chỉ để chuyển từ 30 sang 20 phút.
- [ ] **E3 P1** Chọn database demo riêng có thể tạo lại theo hướng dẫn; khởi động lại bình thường phải giữ dữ liệu. Hiện seed chỉ chạy khi chưa có user, lịch mẫu cũ có thể hết hạn.
- [ ] **E4 P1** Chuẩn bị phiên trình duyệt riêng cho Patient, Staff, Doctor và mở Desktop cho Admin; tránh đổi tài khoản liên tục trong cùng cookie/session.
- [ ] **E5 P1** Chuẩn bị bằng chứng nhắc trước 1 giờ bằng test đồng hồ có kiểm soát và dữ liệu/sự kiện cụ thể; nếu cần tua thời gian trực tiếp, thiết kế chế độ demo riêng và ghi rõ. Không cần chờ thật 1 giờ trong buổi báo cáo.
- [ ] **E6 P1** Sửa nhận diện và hướng dẫn repo: `codes/README.md` còn URL clone repo CSharp cũ; README gốc còn dòng môn CSharp. Đổi sang repo/nhánh thực tế và ngữ cảnh OOAD, giữ các chỉnh sửa README hiện có của người dùng.

## 5. Thứ tự thực hiện và khối lượng dự kiến

| Chặng | Công việc | Điều kiện hoàn tất | Ước lượng công sức |
|---|---|---|---|
| 1 | Kiểm tra chạy nền, thống nhất ánh xạ và schema | Build/chạy nền được; có thiết kế migration, quy tắc chuyển dữ liệu | 0,5–1 ngày công |
| 2 | A2–A4, B1: chuyên khoa đơn, dữ liệu lịch và khung 20 phút | Database thử tạo được; ràng buộc và sinh ca cơ bản đúng | 1,5–2,5 ngày công |
| 3 | B2–B8, A7: nghiệp vụ lịch đầy đủ | Đạt các nhánh tạo/sửa/gia hạn/đóng/mở và rollback | 3–5 ngày công |
| 4 | A5–A6, A8, C: sự kiện, nhắc và rà bảo toàn dữ liệu | Truy vết và thông báo nhất quán; các biên và cạnh tranh đạt | 1,5–2,5 ngày công |
| 5 | Giao diện mới, D, E và hồi quy toàn luồng | Demo nối được bốn vai trò; có bảng truy vết và bằng chứng | 2,5–4 ngày công |

Tổng sơ bộ: 9,5–15 ngày công, gồm triển khai và kiểm thử; đây là ước lượng kỹ thuật, chưa phải lịch cam kết. Chốt lại sau khi chạy baseline và biết hạn demo. Các chặng phụ thuộc schema nên không thể cộng số thành viên để chia đều thời gian một cách trực tiếp.

Nếu thời gian ngắn, vẫn ưu tiên quy ước 20 phút, tạo định kỳ, đóng/mở và các luồng lịch hẹn trước. Các nhánh chưa làm phải được ghi rõ phạm vi demo; không công bố toàn bộ UC10/UC11 đã đạt khi thiếu gia hạn hoặc sửa định kỳ.

## 6. Nghiệm thu sau chỉnh sửa

- [ ] Chạy setup/build và unit/integration/Web tests trên bản sửa; chạy SQL Server tests với kết nối kiểm thử riêng, không tính nhóm bị skipped là đã đạt.
- [ ] Cập nhật test đang xác nhận 30 phút và nhiều chuyên khoa, đặc biệt `AdminAndScheduleTests`; bổ sung test theo yêu cầu mới thay vì chỉ đổi tên test.
- [ ] Test lịch định kỳ gồm ngày biên, tuần không có thứ phù hợp, tạo 8 tuần, chống gửi lại, gia hạn không sinh trùng, giữ ca đóng/chỉnh riêng/bổ sung, mở lại giữ ID, và rollback toàn bộ khi một ngày xung đột.
- [ ] Test FK và dữ liệu lịch sử: không sửa giờ/xóa slot đã tham chiếu; sự kiện khung trước/sau còn truy được; đổi/hủy ngừng nhắc cũ; xử lý lại worker không tạo trùng thông báo.
- [ ] Test biên đúng 2 giờ, đúng giờ bắt đầu, đúng giờ kết thúc; tiếp nhận/không đến cạnh tranh chỉ ra một kết quả cuối hợp lệ.
- [ ] Chạy browser tests và Desktop smoke cho màn hình đã sửa; kiểm tra trạng thái thực trong SQL Server, không chỉ thông báo UI.
- [ ] Đo yêu cầu phi chức năng ở mục 2.2.3: 20 người đồng thời, tối thiểu 5.000 lịch hẹn; p95 đọc/đăng nhập không quá 3 giây, đặt/đổi/hủy không quá 5 giây; tạo lịch một bác sĩ trong 8 tuần không quá 10 giây. Lưu cấu hình máy và kết quả đo.
- [ ] Nếu chưa đo tải, ghi rõ chưa kiểm chứng tiêu chí hiệu năng; tách khỏi kết quả kiểm thử chức năng.
- [ ] Diễn tập từ database demo mới và lặp lại sau restart; kiểm tra dịch vụ, tài khoản, ngày giờ và phương án xem ảnh/video khi thiết bị trục trặc.

## 7. Kịch bản thuyết trình đề xuất trong 12 đến 15 phút

| Thời lượng | Thao tác và điểm chứng minh | Liên hệ báo cáo |
|---|---|---|
| 1 phút | Giới thiệu bài toán, ranh giới và bốn tác nhân | Use case tổng quát; không mở rộng sang điều trị/thanh toán |
| 3 phút | Admin tạo quy tắc hai tuần, preview số ca/slot; minh họa một ca chỉnh riêng và đóng/mở | UC09–UC12; QuyTacLich–MauLapTuan–CaTheoNgay–KhungGio |
| 3 phút | Patient chọn chuyên khoa/bác sĩ/slot, xác nhận, xem mã lịch và thông báo; thử đặt lại slot đã kín | UC05–UC08, UC13–UC15, UC21; include UC14 |
| 2 phút | Đổi lịch giữ mã, giải phóng slot cũ, xem sự kiện; minh họa một lần bị chặn bởi hạn 2 giờ | UC17, BR06–BR07 |
| 2 phút | Staff xác định bệnh nhân, đặt hộ/tiếp nhận; dùng lịch khác để minh họa Không đến | UC16, UC19–UC20; hai kết quả dùng hai lịch khác nhau |
| 1 phút | Doctor chỉ xem lịch của mình; thử truy cập ngoài quyền | BR01 và UC09/UC15 |
| 2–3 phút | Mở một luồng code, bảng dữ liệu/sự kiện và test rollback hoặc chống trùng | Boundary–Control–Entity, transaction, bội số và ràng buộc |

Chuẩn bị thêm thao tác gia hạn và sửa định kỳ để trả lời khi giảng viên hỏi, kể cả khi kịch bản chính không đủ thời gian trình bày tất cả nhánh. Mỗi thành viên cần giải thích được một luồng từ use case đến code và dữ liệu.

## 8. Việc cập nhật báo cáo trước khi nộp

- Cập nhật mục lục tự động: mục lục Chương 5 hiện chưa khớp đầy đủ với các tiêu đề và phần mô tả lớp trong thân tài liệu.
- Thống nhất tên `LichLamViec` dùng trên lifeline tổng quát ở sequence với nhóm `QuyTacLich`, `MauLapTuan`, `CaTheoNgay` ở sơ đồ lớp; giải thích đây là nhóm dữ liệu nếu vẫn giữ tên tổng quát.
- Bổ sung bảng ánh xạ mô hình phân tích và triển khai; ảnh giao diện chạy thật và bằng chứng kiểm thử cho các luồng chọn demo.
- Chỉ cập nhật trạng thái hoàn tất sau khi có kết quả trên phiên bản cuối. Các tài liệu/sơ đồ cũ ở `codes/docs` và `report` cần được ghi rõ phiên bản để tránh dùng nhầm thay cho báo cáo lần 04.

## 9. Bước triển khai đầu tiên

Chạy baseline trên database riêng, sau đó làm A1–A4 và B1. Khi mô hình ca theo ngày, chuyên khoa và khung 20 phút đã thống nhất mới triển khai lịch định kỳ và các màn hình mới. Kế hoạch này chưa thay đổi mã nguồn nghiệp vụ hay báo cáo gốc.

## 10. Đóng gói Docker Compose

Theo yêu cầu bổ sung, đã thêm `compose.yaml`, `.env.example`, Dockerfile nhiều giai đoạn cho API/Web và hướng dẫn `codes/docs/instructions/docker-compose-demo.md`.

- SQL Server, API và Web chạy trong Linux containers; WinForms Admin chạy Windows, kết nối API qua cổng host.
- Có healthcheck và thứ tự SQL → API → Web; API tự migration và seed khi database mới.
- Database và khóa cookie Web dùng named volumes; stop/down thông thường giữ dữ liệu.
- Cấu hình riêng ở `.env`, Git bỏ qua; Docker build context loại cấu hình riêng và file build/cache.
- [ ] Nghiệm thu build image, khởi động đủ ba dịch vụ healthy, luồng bốn vai trò và dữ liệu sau restart trên Docker engine đang chạy.
- [ ] Khi hoàn thành các chặng OOAD, build lại image và chạy hồi quy trên Compose trước buổi demo.

Việc đóng gói giữ nghiệp vụ hiện tại; các hạng mục A–E vẫn cần được triển khai.

Kiểm tra lúc đóng gói ngày 18/11/2026: `docker compose --env-file .env.example config --quiet` thành công. Đã thử khởi động Docker Desktop và build; build chưa thực hiện được vì không có pipe engine `dockerDesktopLinuxEngine` dù Desktop báo đang chạy. Chưa xác nhận build image, container healthy hoặc dữ liệu sau restart. Chạy lại nghiệm thu khi Docker engine sẵn sàng.
