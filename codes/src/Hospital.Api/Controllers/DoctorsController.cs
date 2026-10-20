using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Patient,Staff,Admin")]
public class DoctorsController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public DoctorsController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>
    /// UC06 — Tra cứu danh sách bác sĩ (lọc theo chuyên khoa, từ khóa)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DoctorDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctors(
        [FromQuery] int? specialtyId,
        [FromQuery] string? keyword,
        [FromQuery] PageQuery pagination,
        [FromQuery] bool onlyActive = true)
    {
        var result = await _catalogService.GetDoctorsAsync(specialtyId, keyword, onlyActive, pagination);
        return Ok(ApiResponse<PagedResult<DoctorDto>>.Succeeded(result));
    }

    /// <summary>
    /// UC07 — Xem thông tin chi tiết bác sĩ và ngày khám khả dụng
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDoctorById(Guid id)
    {
        var result = await _catalogService.GetDoctorByIdAsync(id);
        return Ok(ApiResponse<DoctorDetailDto>.Succeeded(result));
    }
}
