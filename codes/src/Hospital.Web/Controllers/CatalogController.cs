using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Catalog;
using Hospital.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[Authorize(Roles = "Patient,Staff,Admin")]
public class CatalogController(PortalApi api) : Controller
{
    public async Task<IActionResult> Index(string? keyword, int? specialtyId, int page = 1)
    {
        ViewData["Keyword"] = keyword;
        ViewData["SpecialtyId"] = specialtyId;
        if (specialtyId.HasValue) ViewData["SpecialtyName"] = (await api.GetAsync<SpecialtyDto>($"api/specialties/{specialtyId}")).Name;
        return View(await api.GetAsync<PagedResult<DoctorDto>>($"api/doctors?page={Math.Clamp(page, 1, 1000000)}&pageSize=9&specialtyId={specialtyId}&keyword={Uri.EscapeDataString(keyword ?? "")}"));
    }
    public async Task<IActionResult> Doctor(Guid id) => View(await api.GetAsync<DoctorDetailDto>($"api/doctors/{id}"));
    public async Task<IActionResult> Specialties(int page = 1) => View(await api.GetAsync<PagedResult<SpecialtyDto>>($"api/specialties?page={Math.Clamp(page, 1, 1000000)}&pageSize=12"));
}
