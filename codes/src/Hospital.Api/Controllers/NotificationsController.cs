using Hospital.Application.Common.Models;
using Hospital.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController, Route("api/notifications"), Authorize(Roles = "Patient")]
public class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery pagination, [FromQuery] bool unreadOnly = false) =>
        Ok(ApiResponse<PagedResult<NotificationDto>>.Succeeded(await service.ListAsync(pagination, unreadOnly)));

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id)
    {
        await service.ReadAsync(id);
        return Ok(ApiResponse.Ok("Đã đọc thông báo."));
    }
}
