using Hospital.Application.Common.Interfaces;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Schedules;
using Hospital.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeSlotsController : ControllerBase
{
    private readonly IScheduleService _scheduleService;
    private readonly ICurrentUserService _currentUserService;

    public TimeSlotsController(IScheduleService scheduleService, ICurrentUserService currentUserService)
    {
        _scheduleService = scheduleService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// UC08 — Xem danh sách các khung giờ còn trống (Open) của bác sĩ theo ngày (BR03)
    /// </summary>
    [HttpGet("available"), Authorize(Roles = "Patient,Staff,Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<TimeSlotDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableSlots([FromQuery] Guid doctorId, [FromQuery] DateOnly date)
    {
        var result = await _scheduleService.GetAvailableTimeSlotsAsync(doctorId, date);
        return Ok(ApiResponse<List<TimeSlotDto>>.Succeeded(result));
    }

    /// <summary>
    /// UC12 — Đóng khung giờ khám (Admin chỉ định, kiểm tra BR09)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CloseSlot(Guid id, [FromBody] CloseTimeSlotRequestDto request)
    {
        var adminId = _currentUserService.UserId ?? Guid.Empty;
        await _scheduleService.CloseTimeSlotAsync(adminId, id, request);
        return Ok(ApiResponse.Ok("Đóng khung giờ thành công."));
    }
}
