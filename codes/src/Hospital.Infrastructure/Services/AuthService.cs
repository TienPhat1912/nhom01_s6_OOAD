using Hospital.Application.Common.Exceptions;
using Hospital.Application.Common.Interfaces;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace Hospital.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly HospitalDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        HospitalDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    // UC01 — Đăng ký tài khoản (Bệnh nhân)
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        DatabaseWork.Validate(request);
        ValidatePersonalData(request.DateOfBirth, request.Gender);
        request.Username = request.Username.Trim();
        request.Email = request.Email.Trim().ToLowerInvariant();
        // 1. Kiểm tra trùng username (BR11, BR12)
        var usernameExists = await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower());
        if (usernameExists)
        {
            throw new BusinessRuleException("Tên đăng nhập đã tồn tại trong hệ thống. Vui lòng chọn tên khác.", "USERNAME_ALREADY_EXISTS");
        }

        // 2. Kiểm tra trùng email (BR11)
        var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (emailExists)
        {
            throw new BusinessRuleException("Địa chỉ email đã được đăng ký trong hệ thống.", "EMAIL_ALREADY_EXISTS");
        }

        // 3. Tạo User với vai trò Patient (BR01)
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == (int)UserRole.Patient);
        if (patientRole == null)
        {
            throw new BusinessRuleException("Hệ thống chưa khởi tạo vai trò Bệnh nhân.", "ROLE_NOT_FOUND");
        }

        var user = new User
        {
            Id = userId,
            Username = request.Username.Trim(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Email = request.Email.Trim().ToLower(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            FullName = request.FullName.Trim(),
            RoleId = (int)UserRole.Patient,
            Role = patientRole,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 4. Sinh mã bệnh nhân duy nhất (BR11)
        var patientCode = "BN-" + patientId.ToString("N")[..17].ToUpperInvariant();

        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            PatientCode = patientCode,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        // 5. Ghi AuditLog (BR11)
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = "REGISTER_ACCOUNT",
            EntityType = "User",
            EntityId = userId.ToString(),
            NewValues = $"Đăng ký tài khoản bệnh nhân: {user.Username} - {patientCode}",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user);
        await _context.Patients.AddAsync(patient);
        await _context.AuditLogs.AddAsync(auditLog);
        await _context.SaveChangesAsync();

        var token = _jwtTokenGenerator.GenerateToken(user, patientId: patientId);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = patientRole.Name,
            PatientId = patientId,
            ExpiresAt = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo
        };
    }

    // UC02 — Đăng nhập (Tất cả vai trò)
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        DatabaseWork.Validate(request);
        var username = request.Username.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Patient)
            .Include(u => u.Doctor)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new BusinessRuleException("Tên đăng nhập hoặc mật khẩu không chính xác.", "INVALID_CREDENTIALS");
        }

        // Kiểm tra trạng thái tài khoản (BR12)
        if (!user.IsActive)
        {
            throw new BusinessRuleException("Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động. Vui lòng liên hệ quản trị viên.", "ACCOUNT_INACTIVE");
        }

        // Cập nhật thời điểm đăng nhập cuối
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        Guid? patientId = user.Patient?.Id;
        Guid? doctorId = user.Doctor?.Id;

        var token = _jwtTokenGenerator.GenerateToken(user, patientId: patientId, doctorId: doctorId);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.Name,
            PatientId = patientId,
            DoctorId = doctorId,
            ExpiresAt = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo
        };
    }

    // UC03 — Xem thông tin cá nhân
    public async Task<UserProfileDto> GetProfileAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Patient)
            .Include(u => u.Doctor)
                .ThenInclude(d => d!.Specialty)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        if (!user.IsActive) throw new ForbiddenAccessException("Tài khoản không hoạt động.");

        var profile = new UserProfileDto
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.Name,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        if (user.Patient != null)
        {
            profile.PatientId = user.Patient.Id;
            profile.PatientCode = user.Patient.PatientCode;
            profile.DateOfBirth = user.Patient.DateOfBirth;
            profile.Gender = user.Patient.Gender;
            profile.Address = user.Patient.Address;
        }

        if (user.Doctor != null)
        {
            profile.DoctorId = user.Doctor.Id;
            profile.DoctorCode = user.Doctor.DoctorCode;
            profile.Title = user.Doctor.Title;
            profile.Bio = user.Doctor.Bio;
            profile.AvatarUrl = user.Doctor.AvatarUrl;
            profile.SpecialtyName = user.Doctor.Specialty.Name;
        }

        return profile;
    }

    // UC03 — Cập nhật thông tin cá nhân
    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request)
    {
        DatabaseWork.Validate(request);
        if (request.DateOfBirth.HasValue) ValidatePersonalData(request.DateOfBirth.Value, request.Gender ?? Gender.Other);
        else if (request.Gender.HasValue && !Enum.IsDefined(request.Gender.Value))
            throw new BusinessRuleException("Giới tính không hợp lệ.");
        request.Email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Patient)
            .Include(u => u.Doctor)
                .ThenInclude(d => d!.Specialty)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        if (!user.IsActive) throw new ForbiddenAccessException();
        var previous = new { user.FullName, user.Email, user.PhoneNumber };

        // Kiểm tra nếu đổi email thì không trùng với người khác (BR11)
        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != userId);
            if (emailExists)
            {
                throw new BusinessRuleException("Địa chỉ email đã được sử dụng bởi tài khoản khác.", "EMAIL_ALREADY_EXISTS");
            }
            user.Email = request.Email.Trim().ToLower();
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        if (user.Patient != null)
        {
            if (request.DateOfBirth.HasValue) user.Patient.DateOfBirth = request.DateOfBirth.Value;
            if (request.Gender.HasValue) user.Patient.Gender = request.Gender.Value;
            if (request.Address != null) user.Patient.Address = request.Address.Trim();
            user.Patient.UpdatedAt = DateTime.UtcNow;
        }

        if (user.Doctor != null)
        {
            user.Doctor.FullName = user.FullName;
            if (request.Title != null) user.Doctor.Title = request.Title.Trim();
            if (request.Bio != null) user.Doctor.Bio = request.Bio.Trim();
            user.Doctor.UpdatedAt = DateTime.UtcNow;
        }

        _context.Audit(userId, "UPDATE_PROFILE", "User", userId, previous,
            new { user.FullName, user.Email, user.PhoneNumber });
        await _context.SaveChangesAsync();

        return await GetProfileAsync(userId);
    }

    // UC04 — Đổi mật khẩu
    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request)
    {
        DatabaseWork.Validate(request);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }
        if (!user.IsActive) throw new ForbiddenAccessException();

        // Kiểm tra mật khẩu cũ (BR12)
        if (!_passwordHasher.VerifyPassword(request.OldPassword, user.PasswordHash))
        {
            throw new BusinessRuleException("Mật khẩu hiện tại không chính xác.", "INVALID_CURRENT_PASSWORD");
        }

        if (request.OldPassword == request.NewPassword)
        {
            throw new BusinessRuleException("Mật khẩu mới không được trùng với mật khẩu cũ.", "PASSWORD_REUSED");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.TokenVersion++;
        user.UpdatedAt = DateTime.UtcNow;

        // Ghi AuditLog
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = "CHANGE_PASSWORD",
            EntityType = "User",
            EntityId = userId.ToString(),
            NewValues = "Người dùng đổi mật khẩu thành công",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    private static void ValidatePersonalData(DateOnly birthDate, Gender gender)
    {
        var today = new ClinicTime(TimeProvider.System).Today;
        if (birthDate == default || birthDate > today || birthDate < today.AddYears(-130) || !Enum.IsDefined(gender))
            throw new BusinessRuleException("Ngày sinh hoặc giới tính không hợp lệ.", "INVALID_PERSONAL_DATA");
    }
}
