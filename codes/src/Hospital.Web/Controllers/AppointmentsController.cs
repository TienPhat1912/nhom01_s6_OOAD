using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Appointments;
using Hospital.Domain.Enums;
using Hospital.Web.Models;
using Hospital.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[Authorize(Roles = "Patient,Doctor,Staff")]
public class AppointmentsController(PortalApi api) : Controller
{
    public async Task<IActionResult> Index(string? keyword, AppointmentStatus? status, DateOnly? fromDate,
        DateOnly? toDate, Guid? doctorId, bool descending = false, int page = 1)
    {
        if (fromDate > toDate) ModelState.AddModelError("", "Ngày kết thúc phải từ ngày bắt đầu trở đi.");
        if (!ModelState.IsValid) return View(new AppointmentListModel(new([], 0, 1, 10), keyword, status, fromDate, toDate, doctorId, descending));
        var endpoint = User.IsInRole("Patient") ? "api/appointments/my" : User.IsInRole("Doctor") ? "api/appointments/doctor" : "api/appointments";
        var query = $"{endpoint}?page={Math.Clamp(page, 1, 1000000)}&pageSize=10&keyword={Uri.EscapeDataString(keyword ?? "")}&status={status}&fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&doctorId={doctorId}&descending={descending}";
        if (doctorId.HasValue && !User.IsInRole("Doctor"))
            ViewData["DoctorName"] = (await api.GetAsync<Hospital.Application.DTOs.Catalog.DoctorDetailDto>($"api/doctors/{doctorId}")).FullName;
        return View(new AppointmentListModel(await api.GetAsync<PagedResult<AppointmentDto>>(query), keyword, status, fromDate, toDate, doctorId, descending));
    }

    public async Task<IActionResult> Detail(Guid id) => View(await api.GetAsync<AppointmentDto>($"api/appointments/{id}"));
}
