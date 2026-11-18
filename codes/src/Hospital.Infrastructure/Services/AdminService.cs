using Hospital.Application.Common.Exceptions;
using Hospital.Application.Common.Interfaces;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Admin;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Services;

public sealed class AdminService(HospitalDbContext db, ICurrentUserService actor, IPasswordHasher hasher,
    ICatalogService catalog, ClinicTime clock) : IAdminService
{
    public Task<PagedResult<UserDto>> UsersAsync(PageQuery page, string? keyword)
    {
        actor.RequireRole("Admin");
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(u => u.Username.Contains(k) || u.FullName.Contains(k) || u.Email.Contains(k));
        }
        return query.OrderBy(u => u.Username).ThenBy(u => u.Id).Select(u => new UserDto(u.Id, u.Username,
            u.FullName, u.Email, u.PhoneNumber, (UserRole)u.RoleId, u.IsActive,
            u.Doctor == null ? null : (Guid?)u.Doctor.Id)).PageAsync(page);
    }

    public async Task<UserDto> UserAsync(Guid id)
    {
        actor.RequireRole("Admin");
        var user = await db.Users.AsNoTracking().Include(u => u.Doctor).SingleOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException("Tài khoản", id);
        return Map(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        var adminId = actor.RequireRole("Admin");
        DatabaseWork.Validate(request);
        if (request.Role == UserRole.Patient || !Enum.IsDefined(request.Role))
            throw new BusinessRuleException("Chỉ cấp tài khoản bác sĩ, nhân viên hoặc quản trị viên.");
        return await db.AtomicAsync(async () =>
        {
            await CheckIdentityAsync(request.Username, request.Email);
            var user = new User
            {
                Id = Guid.NewGuid(), Username = request.Username.Trim(), Email = request.Email.Trim().ToLowerInvariant(),
                FullName = request.FullName.Trim(), PhoneNumber = request.PhoneNumber?.Trim(),
                PasswordHash = hasher.HashPassword(request.Password), RoleId = (int)request.Role
            };
            await LinkDoctorAsync(user, request.Role, request.DoctorId);
            db.Users.Add(user);
            db.Audit(adminId, "CREATE_USER", "User", user.Id, newValue: new { user.Username, user.RoleId });
            return Map(user);
        });
    }

    public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var adminId = actor.RequireRole("Admin");
        DatabaseWork.Validate(request);
        return await db.AtomicAsync(async () =>
        {
            var user = await db.Users.Include(u => u.Doctor).SingleOrDefaultAsync(u => u.Id == id)
                ?? throw new NotFoundException("Tài khoản", id);
            await CheckIdentityAsync(user.Username, request.Email, id);
            if (!request.IsActive) await ProtectLastAdminAsync(user);
            if (!request.IsActive && user.Doctor != null) await CheckFutureAppointmentsAsync(user.Doctor.Id);
            var old = new { user.FullName, user.Email, user.IsActive };
            if (user.IsActive != request.IsActive) user.TokenVersion++;
            user.FullName = request.FullName.Trim(); user.Email = request.Email.Trim().ToLowerInvariant();
            user.PhoneNumber = request.PhoneNumber?.Trim(); user.IsActive = request.IsActive;
            user.UpdatedAt = clock.UtcNow;
            if (user.Doctor != null) user.Doctor.FullName = user.FullName;
            db.Audit(adminId, "UPDATE_USER", "User", id, old, new { user.FullName, user.Email, user.IsActive });
            return Map(user);
        });
    }

    public async Task<UserDto> ChangeRoleAsync(Guid id, ChangeRoleRequest request)
    {
        var adminId = actor.RequireRole("Admin");
        if (!Enum.IsDefined(request.Role)) throw new BusinessRuleException("Vai trò không hợp lệ.");
        return await db.AtomicAsync(async () =>
        {
            var user = await db.Users.Include(u => u.Doctor).Include(u => u.Patient).SingleOrDefaultAsync(u => u.Id == id)
                ?? throw new NotFoundException("Tài khoản", id);
            if (request.Role != UserRole.Admin) await ProtectLastAdminAsync(user);
            // Patient accounts keep their clinical ownership; do not manufacture or discard patient profiles.
            if (user.Patient != null && request.Role != UserRole.Patient || user.Patient == null && request.Role == UserRole.Patient)
                throw new BusinessRuleException("Vai trò không phù hợp hồ sơ bệnh nhân.", "ROLE_PROFILE_MISMATCH");
            var old = user.RoleId;
            if (user.Doctor != null && request.Role != UserRole.Doctor)
            {
                await CheckFutureAppointmentsAsync(user.Doctor.Id);
                user.Doctor.UserId = null;
                user.Doctor.User = null;
                user.Doctor = null;
            }
            await LinkDoctorAsync(user, request.Role, request.DoctorId);
            user.RoleId = (int)request.Role; user.TokenVersion++; user.UpdatedAt = clock.UtcNow;
            db.Audit(adminId, "CHANGE_ROLE", "User", id, new { RoleId = old }, new { user.RoleId });
            return Map(user);
        });
    }

    private async Task LinkDoctorAsync(User user, UserRole role, Guid? doctorId)
    {
        if (role != UserRole.Doctor)
        {
            if (doctorId.HasValue) throw new BusinessRuleException("Chỉ vai trò bác sĩ được liên kết hồ sơ bác sĩ.");
            return;
        }
        doctorId ??= user.Doctor?.Id;
        var doctor = await db.Doctors.SingleOrDefaultAsync(d => d.Id == doctorId && d.IsActive)
            ?? throw new BusinessRuleException("Cần hồ sơ bác sĩ đang hoạt động trước khi cấp quyền.", "DOCTOR_PROFILE_REQUIRED");
        if (doctor.UserId.HasValue && doctor.UserId != user.Id)
            throw new BusinessRuleException("Hồ sơ bác sĩ đã liên kết tài khoản khác.");
        if (user.Doctor != null && user.Doctor.Id != doctor.Id)
            throw new BusinessRuleException("Không được chuyển tài khoản sang hồ sơ bác sĩ khác.");
        doctor.UserId = user.Id; doctor.User = user; user.Doctor = doctor;
        user.FullName = doctor.FullName;
    }

    private async Task ProtectLastAdminAsync(User user)
    {
        if (user.IsActive && user.RoleId == (int)UserRole.Admin
            && await db.Users.CountAsync(u => u.IsActive && u.RoleId == (int)UserRole.Admin) <= 1)
            throw new BusinessRuleException("Không thể khóa hoặc thu hồi quyền quản trị viên hoạt động cuối cùng.", "LAST_ADMIN_PROTECTED");
    }

    private async Task CheckIdentityAsync(string username, string email, Guid? excluded = null)
    {
        var name = username.Trim().ToLowerInvariant(); var address = email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Id != excluded && (u.Username.ToLower() == name || u.Email.ToLower() == address)))
            throw new BusinessRuleException("Tên đăng nhập hoặc email đã tồn tại.", "IDENTITY_ALREADY_EXISTS");
    }

    public Task<PagedResult<UserProfileDto>> PatientsAsync(PageQuery page, string? keyword)
    {
        actor.RequireRole("Staff");
        var query = db.Patients.AsNoTracking().Where(p => p.User.IsActive && p.User.RoleId == (int)UserRole.Patient);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(p => p.PatientCode.Contains(k) || p.User.FullName.Contains(k)
                || p.User.PhoneNumber != null && p.User.PhoneNumber.Contains(k));
        }
        return query.OrderBy(p => p.PatientCode).Select(p => new UserProfileDto
        {
            UserId = p.UserId, PatientId = p.Id, PatientCode = p.PatientCode, FullName = p.User.FullName,
            PhoneNumber = p.User.PhoneNumber, DateOfBirth = p.DateOfBirth, Gender = p.Gender, Address = p.Address,
            Role = "Patient", IsActive = p.User.IsActive
        }).PageAsync(page);
    }

    public async Task<UserProfileDto> PatientAsync(Guid id)
    {
        actor.RequireRole("Staff");
        return await db.Patients.AsNoTracking()
            .Where(p => p.Id == id && p.User.IsActive && p.User.RoleId == (int)UserRole.Patient)
            .Select(p => new UserProfileDto
            {
                UserId = p.UserId, PatientId = p.Id, PatientCode = p.PatientCode, FullName = p.User.FullName,
                PhoneNumber = p.User.PhoneNumber, DateOfBirth = p.DateOfBirth, Gender = p.Gender,
                Address = p.Address, Role = "Patient", IsActive = p.User.IsActive
            }).SingleOrDefaultAsync() ?? throw new NotFoundException("Bệnh nhân", id);
    }

    public async Task<DoctorDetailDto> SaveDoctorAsync(Guid? id, SaveDoctorRequest request)
    {
        var adminId = actor.RequireRole("Admin");
        DatabaseWork.Validate(request);
        var doctorId = await db.AtomicAsync(async () =>
        {
            if (!await db.Specialties.AnyAsync(s => s.Id == request.SpecialtyId && s.IsActive))
                throw new BusinessRuleException("Chuyên khoa không tồn tại hoặc không hoạt động.");
            var doctor = id.HasValue ? await db.Doctors.Include(d => d.User)
                .SingleOrDefaultAsync(d => d.Id == id) ?? throw new NotFoundException("Bác sĩ", id) :
                new Doctor { Id = Guid.NewGuid(), CreatedBy = adminId };
            var old = new { doctor.FullName, doctor.IsActive, doctor.SpecialtyId };
            if (id.HasValue && (!request.IsActive || doctor.SpecialtyId != request.SpecialtyId))
                await CheckFutureAppointmentsAsync(doctor.Id);
            if (!id.HasValue) { doctor.DoctorCode = "BS-" + doctor.Id.ToString("N")[..17].ToUpperInvariant(); db.Doctors.Add(doctor); }
            doctor.FullName = request.FullName.Trim(); doctor.Title = request.Title?.Trim(); doctor.Bio = request.Bio?.Trim();
            if (request.AvatarUrl != null && !IsSafeAvatar(request.AvatarUrl))
                throw new BusinessRuleException("Ảnh phải dùng đường dẫn /images/ hoặc URL HTTPS.");
            doctor.AvatarUrl = request.AvatarUrl; doctor.IsActive = request.IsActive;
            doctor.UpdatedAt = id.HasValue ? clock.UtcNow : null;
            if (doctor.User != null) doctor.User.FullName = doctor.FullName;
            doctor.SpecialtyId = request.SpecialtyId;
            db.Audit(adminId, id.HasValue ? "UPDATE_DOCTOR" : "CREATE_DOCTOR", "Doctor", doctor.Id, old,
                new { doctor.FullName, doctor.IsActive, doctor.SpecialtyId });
            return doctor.Id;
        });
        return await catalog.GetDoctorByIdAsync(doctorId);
    }

    private static bool IsSafeAvatar(string value) => value.StartsWith("/images/", StringComparison.Ordinal)
        || Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

    private async Task CheckFutureAppointmentsAsync(Guid doctorId)
    {
        var date = clock.Today; var time = TimeOnly.FromDateTime(clock.LocalNow);
        if (await db.Appointments.AnyAsync(a => a.DoctorId == doctorId && a.Status == AppointmentStatus.Confirmed
            && (a.AppointmentDate > date || a.AppointmentDate == date && a.EndTime > time)))
            throw new BusinessRuleException("Còn lịch Đã xác nhận bị ảnh hưởng; nhân viên cần đổi/hủy trước.", "ACTIVE_APPOINTMENTS_EXIST");
    }

    public async Task<SpecialtyDto> SaveSpecialtyAsync(int? id, SaveSpecialtyRequest request)
    {
        var adminId = actor.RequireRole("Admin");
        DatabaseWork.Validate(request);
        var specialtyId = await db.AtomicAsync(async () =>
        {
            var code = request.Code.Trim().ToUpperInvariant(); var name = request.Name.Trim();
            if (await db.Specialties.AnyAsync(s => s.Id != id && (s.Code == code || s.Name.ToLower() == name.ToLower())))
                throw new BusinessRuleException("Mã hoặc tên chuyên khoa đã tồn tại.", "SPECIALTY_ALREADY_EXISTS");
            var s = id.HasValue ? await db.Specialties.SingleOrDefaultAsync(s => s.Id == id)
                ?? throw new NotFoundException("Chuyên khoa", id) : new Specialty { CreatedBy = adminId };
            var date = clock.Today; var time = TimeOnly.FromDateTime(clock.LocalNow);
            if (!request.IsActive && id.HasValue && await db.Appointments.AnyAsync(a => a.SpecialtyId == id
                && a.Status == AppointmentStatus.Confirmed && (a.AppointmentDate > date || a.AppointmentDate == date && a.EndTime > time)))
                throw new BusinessRuleException("Chuyên khoa còn lịch Đã xác nhận bị ảnh hưởng.", "ACTIVE_APPOINTMENTS_EXIST");
            var old = new { s.Code, s.Name, s.IsActive };
            s.Code = code; s.Name = name; s.Description = request.Description?.Trim(); s.IsActive = request.IsActive;
            s.UpdatedAt = id.HasValue ? clock.UtcNow : null;
            if (!id.HasValue) db.Specialties.Add(s);
            await db.SaveChangesAsync();
            db.Audit(adminId, id.HasValue ? "UPDATE_SPECIALTY" : "CREATE_SPECIALTY", "Specialty", s.Id, old,
                new { s.Code, s.Name, s.IsActive });
            return s.Id;
        });
        return await catalog.GetSpecialtyByIdAsync(specialtyId);
    }

    private static UserDto Map(User u) => new(u.Id, u.Username, u.FullName, u.Email, u.PhoneNumber,
        (UserRole)u.RoleId, u.IsActive, u.Doctor?.Id);
}
