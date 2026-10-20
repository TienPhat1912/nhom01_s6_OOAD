using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Patient,Staff,Admin")]
public class SpecialtiesController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public SpecialtiesController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>
    /// UC05 — Tra cứu danh sách chuyên khoa
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SpecialtyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpecialties([FromQuery] string? keyword, [FromQuery] PageQuery pagination, [FromQuery] bool onlyActive = true)
    {
        var result = await _catalogService.GetSpecialtiesAsync(keyword, onlyActive, pagination);
        return Ok(ApiResponse<PagedResult<SpecialtyDto>>.Succeeded(result));
    }

    /// <summary>
    /// UC05 — Xem thông tin chuyên khoa theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpecialtyById(int id)
    {
        var result = await _catalogService.GetSpecialtyByIdAsync(id);
        return Ok(ApiResponse<SpecialtyDto>.Succeeded(result));
    }
}
