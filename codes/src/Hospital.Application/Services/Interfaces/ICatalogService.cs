using Hospital.Application.DTOs.Catalog;
using Hospital.Application.Common.Models;

namespace Hospital.Application.Services.Interfaces;

public interface ICatalogService
{
    Task<PagedResult<SpecialtyDto>> GetSpecialtiesAsync(string? keyword = null, bool onlyActive = true, PageQuery? page = null);
    Task<SpecialtyDto> GetSpecialtyByIdAsync(int id);
    Task<PagedResult<DoctorDto>> GetDoctorsAsync(int? specialtyId = null, string? keyword = null, bool onlyActive = true, PageQuery? page = null);
    Task<DoctorDetailDto> GetDoctorByIdAsync(Guid id);
}
