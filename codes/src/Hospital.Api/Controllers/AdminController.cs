using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.Services.Interfaces;
using Hospital.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Api.Controllers;

[ApiController, Route("api/admin"), Authorize(Roles = "Admin")]
public class AdminController(IAdminService service, HospitalDbContext db) : ControllerBase
{
    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id) => Ok(ApiResponse<UserDto>.Succeeded(await service.UserAsync(id)));

    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] PageQuery pagination, [FromQuery] string? keyword) =>
        Ok(ApiResponse<PagedResult<UserDto>>.Succeeded(await service.UsersAsync(pagination, keyword)));

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var user = await service.CreateUserAsync(request);
        return StatusCode(201, ApiResponse<UserDto>.Succeeded(user));
    }

    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request) =>
        Ok(ApiResponse<UserDto>.Succeeded(await service.UpdateUserAsync(id, request)));

    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, ChangeRoleRequest request) =>
        Ok(ApiResponse<UserDto>.Succeeded(await service.ChangeRoleAsync(id, request)));

    [HttpPost("doctors")]
    public async Task<IActionResult> CreateDoctor(SaveDoctorRequest request) =>
        StatusCode(201, ApiResponse<DoctorDetailDto>.Succeeded(await service.SaveDoctorAsync(null, request)));

    [HttpPut("doctors/{id:guid}")]
    public async Task<IActionResult> UpdateDoctor(Guid id, SaveDoctorRequest request) =>
        Ok(ApiResponse<DoctorDetailDto>.Succeeded(await service.SaveDoctorAsync(id, request)));

    [HttpPost("specialties")]
    public async Task<IActionResult> CreateSpecialty(SaveSpecialtyRequest request) =>
        StatusCode(201, ApiResponse<SpecialtyDto>.Succeeded(await service.SaveSpecialtyAsync(null, request)));

    [HttpPut("specialties/{id:int}")]
    public async Task<IActionResult> UpdateSpecialty(int id, SaveSpecialtyRequest request) =>
        Ok(ApiResponse<SpecialtyDto>.Succeeded(await service.SaveSpecialtyAsync(id, request)));

    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs([FromQuery] PageQuery pagination, [FromQuery] string? entityType)
    {
        var query = db.AuditLogs.AsNoTracking().Where(a => entityType == null || a.EntityType == entityType);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize)
            .Select(a => new AuditLogDto(a.Id, a.UserId, a.User == null ? null : a.User.FullName,
                a.Action, a.EntityType, a.EntityId, a.OldValues, a.NewValues, a.CreatedAt)).ToListAsync();
        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Succeeded(new(items, total, pagination.Page, pagination.PageSize)));
    }
}

[ApiController, Route("api/patients"), Authorize(Roles = "Staff")]
public class PatientsController(IAdminService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id) =>
        Ok(ApiResponse<UserProfileDto>.Succeeded(await service.PatientAsync(id)));

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] PageQuery pagination, [FromQuery] string? keyword) =>
        Ok(ApiResponse<PagedResult<UserProfileDto>>.Succeeded(await service.PatientsAsync(pagination, keyword)));
}
