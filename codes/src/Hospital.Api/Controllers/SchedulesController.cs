using Hospital.Application.Common.Interfaces;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Schedules;
using Hospital.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _scheduleService;
    private readonly ICurrentUserService _currentUserService;

    public SchedulesController(IScheduleService scheduleService, ICurrentUserService currentUserService)
    {
        _scheduleService = scheduleService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// UC09 — Xem lịch làm việc (Bác sĩ xem của mình, Admin xem danh sách theo bộ lọc)
    /// </summary>
    [Authorize(Roles = "Admin,Doctor")]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<WorkingScheduleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedules(
        [FromQuery] Guid? doctorId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        // BR01 & BR02: Bác sĩ chỉ xem được lịch của bản thân
        if (_currentUserService.Role == "Doctor")
        {
            if (!_currentUserService.DoctorId.HasValue)
            {
                return Forbid();
            }
            doctorId = _currentUserService.DoctorId.Value;
        }

        var result = await _scheduleService.GetWorkingSchedulesAsync(doctorId, fromDate, toDate);
        return Ok(ApiResponse<List<WorkingScheduleDto>>.Succeeded(result));
    }

    /// <summary>
    /// UC09 — Bác sĩ xem ca trực của bản thân
    /// </summary>
    [Authorize(Roles = "Doctor")]
    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<List<WorkingScheduleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySchedules([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        if (!_currentUserService.DoctorId.HasValue)
        {
            return Forbid();
        }

        var result = await _scheduleService.GetWorkingSchedulesAsync(_currentUserService.DoctorId.Value, fromDate, toDate);
        return Ok(ApiResponse<List<WorkingScheduleDto>>.Succeeded(result));
    }

    /// <summary>
    /// UC09 — Xem chi tiết một ca làm việc
    /// </summary>
    [Authorize(Roles = "Admin,Doctor")]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkingScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScheduleById(Guid id)
    {
        var result = await _scheduleService.GetWorkingScheduleByIdAsync(id);

        if (_currentUserService.Role == "Doctor" && result.DoctorId != _currentUserService.DoctorId)
        {
            return Forbid();
        }

        return Ok(ApiResponse<WorkingScheduleDto>.Succeeded(result));
    }

    /// <summary>
    /// UC10 — Tạo ca làm việc mới và tự động sinh TimeSlots (Chỉ Quản trị viên)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WorkingScheduleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateWorkingScheduleRequestDto request)
    {
        var adminId = _currentUserService.UserId ?? Guid.Empty;
        var result = await _scheduleService.CreateWorkingScheduleAsync(adminId, request);
        return CreatedAtAction(nameof(GetScheduleById), new { id = result.Id }, ApiResponse<WorkingScheduleDto>.Succeeded(result, "Tạo lịch làm việc thành công!"));
    }

    /// <summary>
    /// UC11 — Điều chỉnh ca làm việc (Chỉ Quản trị viên, kiểm tra BR09)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkingScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSchedule(Guid id, [FromBody] UpdateWorkingScheduleRequestDto request)
    {
        var adminId = _currentUserService.UserId ?? Guid.Empty;
        var result = await _scheduleService.UpdateWorkingScheduleAsync(adminId, id, request);
        return Ok(ApiResponse<WorkingScheduleDto>.Succeeded(result, "Cập nhật ca làm việc thành công!"));
    }
}
