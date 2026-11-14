using System.ComponentModel.DataAnnotations;
using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs.Admin;

public class CreateUserRequest
{
    [Required, StringLength(50, MinimumLength = 3)] public string Username { get; set; } = "";
    [Required, StringLength(72, MinimumLength = 8)] public string Password { get; set; } = "";
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public Guid? DoctorId { get; set; }
}

public class UpdateUserRequest
{
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ChangeRoleRequest
{
    public UserRole Role { get; set; }
    public Guid? DoctorId { get; set; }
}

public record UserDto(Guid Id, string Username, string FullName, string Email, string? PhoneNumber,
    UserRole Role, bool IsActive, Guid? DoctorId);

public record AuditLogDto(long Id, Guid? UserId, string? UserName, string Action, string EntityType,
    string? EntityId, string? OldValues, string? NewValues, DateTime CreatedAt);

public class SaveDoctorRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [StringLength(50)] public string? Title { get; set; }
    [StringLength(1000)] public string? Bio { get; set; }
    [StringLength(500)] public string? AvatarUrl { get; set; }
    [Required, MinLength(1)] public List<int> SpecialtyIds { get; set; } = [];
    public int PrimarySpecialtyId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveSpecialtyRequest
{
    [Required, StringLength(20)] public string Code { get; set; } = "";
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [StringLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
