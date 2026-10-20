using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Appointments;
using Hospital.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController, Route("api/appointments"), Authorize(Roles = "Patient,Doctor,Staff")]
public class AppointmentsController(IAppointmentService service) : ControllerBase
{
    [HttpGet, Authorize(Roles = "Staff")]
    public async Task<IActionResult> List([FromQuery] AppointmentQuery query) =>
        Ok(ApiResponse<PagedResult<AppointmentDto>>.Succeeded(await service.ListAsync(query)));

    [HttpGet("my"), Authorize(Roles = "Patient")]
    public Task<IActionResult> Mine([FromQuery] AppointmentQuery query) => ListScoped(query);

    [HttpGet("doctor"), Authorize(Roles = "Doctor")]
    public Task<IActionResult> Doctor([FromQuery] AppointmentQuery query) => ListScoped(query);

    [HttpGet("search"), Authorize(Roles = "Staff")]
    public Task<IActionResult> Search([FromQuery] AppointmentQuery query) => ListScoped(query);

    private async Task<IActionResult> ListScoped(AppointmentQuery query) =>
        Ok(ApiResponse<PagedResult<AppointmentDto>>.Succeeded(await service.ListAsync(query)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(ApiResponse<AppointmentDto>.Succeeded(await service.GetAsync(id)));

    [HttpPost, Authorize(Roles = "Patient,Staff")]
    public async Task<IActionResult> Book(BookAppointmentRequest request)
    {
        var result = await service.BookAsync(request);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, ApiResponse<AppointmentDto>.Succeeded(result));
    }

    [HttpPost("{id:guid}/reschedule"), Authorize(Roles = "Patient,Staff")]
    public async Task<IActionResult> Reschedule(Guid id, RescheduleAppointmentRequest request) =>
        Ok(ApiResponse<AppointmentDto>.Succeeded(await service.RescheduleAsync(id, request)));

    [HttpPost("{id:guid}/cancel"), Authorize(Roles = "Patient,Staff")]
    public async Task<IActionResult> Cancel(Guid id, AppointmentMutationRequest request) =>
        Ok(ApiResponse<AppointmentDto>.Succeeded(await service.CancelAsync(id, request)));

    [HttpPost("{id:guid}/check-in"), Authorize(Roles = "Staff")]
    public async Task<IActionResult> CheckIn(Guid id, AppointmentMutationRequest request) =>
        Ok(ApiResponse<AppointmentDto>.Succeeded(await service.CheckInAsync(id, request)));

    [HttpPost("{id:guid}/no-show"), Authorize(Roles = "Staff")]
    public async Task<IActionResult> NoShow(Guid id, AppointmentMutationRequest request) =>
        Ok(ApiResponse<AppointmentDto>.Succeeded(await service.NoShowAsync(id, request)));
}
