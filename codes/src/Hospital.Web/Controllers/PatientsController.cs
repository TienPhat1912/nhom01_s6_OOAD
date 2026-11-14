using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Auth;
using Hospital.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

[Authorize(Roles = "Staff")]
public class PatientsController(PortalApi api) : Controller
{
    public async Task<IActionResult> Index(string? keyword, Guid? doctorId, int? specialtyId, int page = 1)
    {
        ViewData["Keyword"] = keyword;
        ViewData["DoctorId"] = doctorId;
        ViewData["SpecialtyId"] = specialtyId;
        return View(await api.GetAsync<PagedResult<UserProfileDto>>(
            $"api/patients?page={Math.Clamp(page, 1, 1000000)}&pageSize=10&keyword={Uri.EscapeDataString(keyword ?? "")}"));
    }
}
