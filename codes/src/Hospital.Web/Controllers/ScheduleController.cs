using Hospital.Application.DTOs.Schedules;
using Hospital.Web.Models;
using Hospital.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[Authorize(Roles = "Doctor")]
public class ScheduleController(PortalApi api) : Controller
{
    public async Task<IActionResult> Index(DateOnly? date)
    {
        var start = date ?? DisplayText.Today;
        ViewData["Date"] = start;
        return View(await api.GetAsync<List<WorkingScheduleDto>>($"api/schedules/my?fromDate={start:yyyy-MM-dd}&toDate={start.AddDays(6):yyyy-MM-dd}"));
    }
}
