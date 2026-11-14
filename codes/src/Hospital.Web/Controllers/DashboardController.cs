using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Appointments;
using Hospital.Application.DTOs.Catalog;
using Hospital.Web.Models;
using Hospital.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[Authorize]
public class DashboardController(PortalApi api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new DashboardModel();
        if (User.IsInRole("Admin"))
        {
            var users = await api.GetAsync<PagedResult<UserDto>>("api/admin/users?pageSize=1");
            var doctors = await api.GetAsync<PagedResult<DoctorDto>>("api/doctors?pageSize=1&onlyActive=false");
            var specialties = await api.GetAsync<PagedResult<SpecialtyDto>>("api/specialties?pageSize=1&onlyActive=false");
            model.Period = "Dữ liệu hệ thống";
            model.Stats = [new("Tài khoản", users.TotalCount, "users", "Tất cả vai trò"),
                new("Bác sĩ", doctors.TotalCount, "heart", "Hồ sơ trong hệ thống"), new("Chuyên khoa", specialties.TotalCount, "grid", "Danh mục hiện có")];
        }
        else
        {
            var today = DisplayText.Today;
            var endpoint = User.IsInRole("Patient") ? "api/appointments/my" : User.IsInRole("Doctor") ? "api/appointments/doctor" : "api/appointments";
            var range = User.IsInRole("Patient") ? $"fromDate={today:yyyy-MM-dd}" : $"fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}";
            var all = await api.GetAsync<PagedResult<AppointmentDto>>($"{endpoint}?{range}&pageSize=6");
            var confirmed = await api.GetAsync<PagedResult<AppointmentDto>>($"{endpoint}?{range}&status=Confirmed&pageSize=1");
            var checkedIn = await api.GetAsync<PagedResult<AppointmentDto>>($"{endpoint}?{range}&status=CheckedIn&pageSize=1");
            model.Appointments = all.Items;
            model.Period = User.IsInRole("Patient") ? "Từ hôm nay" : "Hôm nay";
            model.Stats = [new("Lịch hẹn", all.TotalCount, "calendar", model.Period),
                new("Đã xác nhận", confirmed.TotalCount, "check", "Đang chờ tiếp nhận"), new("Đã tiếp nhận", checkedIn.TotalCount, "users", "Đã ghi nhận đến khám")];
        }
        return View(model);
    }
}
