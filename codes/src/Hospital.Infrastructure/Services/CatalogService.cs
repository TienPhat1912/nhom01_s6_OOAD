using Hospital.Application.Common.Exceptions;
using Hospital.Application.Common.Interfaces;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public class CatalogService(HospitalDbContext db, ICurrentUserService actor, ClinicTime clock) : ICatalogService
{
    private bool Admin => actor.Role == "Admin";
    private void Access(bool onlyActive = true)
    {
        actor.RequireRole("Patient", "Staff", "Admin");
        if (!onlyActive && !Admin) throw new ForbiddenAccessException();
    }

    public Task<PagedResult<SpecialtyDto>> GetSpecialtiesAsync(string? keyword = null, bool onlyActive = true, PageQuery? page = null)
    {
        Access(onlyActive);
        var query = db.Specialties.AsNoTracking().Where(s => !onlyActive || s.IsActive);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(s => s.Name.Contains(k) || s.Code.Contains(k));
        }
        return query.OrderBy(s => s.Name).ThenBy(s => s.Id).Select(s => new SpecialtyDto
        {
            Id = s.Id, Code = s.Code, Name = s.Name, Description = s.Description, IsActive = s.IsActive,
            DoctorCount = s.DoctorSpecialties.Count(ds => ds.Doctor.IsActive && (ds.Doctor.User == null || ds.Doctor.User.IsActive))
        }).PageAsync(page ?? new PageQuery());
    }

    public async Task<SpecialtyDto> GetSpecialtyByIdAsync(int id)
    {
        Access();
        return await db.Specialties.AsNoTracking().Where(s => s.Id == id && (s.IsActive || Admin))
            .Select(s => new SpecialtyDto
            {
                Id = s.Id, Code = s.Code, Name = s.Name, Description = s.Description, IsActive = s.IsActive,
                DoctorCount = s.DoctorSpecialties.Count(ds => ds.Doctor.IsActive && (ds.Doctor.User == null || ds.Doctor.User.IsActive))
            }).SingleOrDefaultAsync() ?? throw new NotFoundException("Chuyên khoa", id);
    }

    public Task<PagedResult<DoctorDto>> GetDoctorsAsync(int? specialtyId = null, string? keyword = null, bool onlyActive = true, PageQuery? page = null)
    {
        Access(onlyActive);
        var query = db.Doctors.AsNoTracking().Where(d => !onlyActive || d.IsActive
            && (d.User == null || d.User.IsActive) && d.DoctorSpecialties.Any(ds => ds.Specialty.IsActive));
        if (specialtyId.HasValue) query = query.Where(d => d.DoctorSpecialties.Any(ds => ds.SpecialtyId == specialtyId && (ds.Specialty.IsActive || Admin)));
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(d => d.FullName.Contains(k) || d.DoctorCode.Contains(k)
                || d.DoctorSpecialties.Any(ds => ds.Specialty.Name.Contains(k) && (ds.Specialty.IsActive || Admin)));
        }
        return query.OrderBy(d => d.FullName).ThenBy(d => d.Id).Select(d => new DoctorDto
        {
            Id = d.Id, DoctorCode = d.DoctorCode, FullName = d.FullName, Title = d.Title, Bio = d.Bio,
            AvatarUrl = d.AvatarUrl, IsActive = d.IsActive, LinkedUserId = Admin ? d.UserId : null,
            Specialties = d.DoctorSpecialties.Where(ds => ds.Specialty.IsActive || Admin).OrderByDescending(ds => ds.IsPrimary)
                .Select(ds => ds.Specialty.Name).ToList()
        }).PageAsync(page ?? new PageQuery());
    }

    public async Task<DoctorDetailDto> GetDoctorByIdAsync(Guid id)
    {
        Access();
        var d = await db.Doctors.AsNoTracking().Include(d => d.User).Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
            .SingleOrDefaultAsync(d => d.Id == id && (Admin || d.IsActive && (d.User == null || d.User.IsActive)
                && d.DoctorSpecialties.Any(ds => ds.Specialty.IsActive))) ?? throw new NotFoundException("Bác sĩ", id);
        var today = clock.Today; var now = TimeOnly.FromDateTime(clock.LocalNow);
        var dates = await db.TimeSlots.Where(s => s.DoctorId == id && s.IsActive && s.Status == TimeSlotStatus.Open
            && (s.SlotDate > today || s.SlotDate == today && s.StartTime > now)
            && !s.Appointments.Any(a => a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.CheckedIn))
            .Select(s => s.SlotDate).Distinct().OrderBy(date => date).ToListAsync();
        return new DoctorDetailDto
        {
            Id = d.Id, DoctorCode = d.DoctorCode, FullName = d.FullName, Title = d.Title, Bio = d.Bio,
            AvatarUrl = d.AvatarUrl, IsActive = d.IsActive,
            PrimarySpecialtyId = d.DoctorSpecialties.Where(ds => ds.IsPrimary).Select(ds => (int?)ds.SpecialtyId).FirstOrDefault(),
            Specialties = d.DoctorSpecialties.Where(ds => ds.Specialty.IsActive || Admin).OrderByDescending(ds => ds.IsPrimary)
                .Select(ds => new SpecialtyDto { Id = ds.SpecialtyId, Name = ds.Specialty.Name, Code = ds.Specialty.Code,
                    IsActive = ds.Specialty.IsActive, Description = ds.Specialty.Description }).ToList(),
            AvailableDates = dates
        };
    }
}
