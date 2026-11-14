using Hospital.Application.Common.Models;
using Hospital.Application.Services.Interfaces;
using Hospital.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[Authorize(Roles = "Patient")]
public class NotificationsController(PortalApi api) : Controller
{
    public async Task<IActionResult> Index(int page = 1) => View(await api.GetAsync<PagedResult<NotificationDto>>($"api/notifications?page={Math.Clamp(page, 1, 1000000)}&pageSize=10"));
    [HttpPost]
    public async Task<IActionResult> Read(Guid id)
    {
        await api.SendAsync<object>(HttpMethod.Put, $"api/notifications/{id}/read", new { });
        TempData["Success"] = "Đã đánh dấu thông báo là đã đọc.";
        return RedirectToAction(nameof(Index));
    }
}
