using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.DTOs.Catalog;

namespace Hospital.Application.Services.Interfaces;

public interface IAdminService
{
    Task<PagedResult<UserDto>> UsersAsync(PageQuery page, string? keyword);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task<UserDto> ChangeRoleAsync(Guid id, ChangeRoleRequest request);
    Task<PagedResult<UserProfileDto>> PatientsAsync(PageQuery page, string? keyword);
    Task<DoctorDetailDto> SaveDoctorAsync(Guid? id, SaveDoctorRequest request);
    Task<SpecialtyDto> SaveSpecialtyAsync(int? id, SaveSpecialtyRequest request);
}
