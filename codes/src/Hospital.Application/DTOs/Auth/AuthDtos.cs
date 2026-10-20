using System.ComponentModel.DataAnnotations;
using Hospital.Domain.Enums;

namespace Hospital.Application.DTOs.Auth;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "Mật khẩu từ 8 đến 72 ký tự, tối đa 72 byte UTF-8")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên không được để trống")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Ngày sinh không được để trống")]
    public DateOnly DateOfBirth { get; set; }

    public Gender Gender { get; set; } = Gender.Other;

    [StringLength(300)] public string? Address { get; set; }
}

public class LoginRequestDto
{
    [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [StringLength(72)]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class ChangePasswordRequestDto
{
    [Required(ErrorMessage = "Mật khẩu cũ không được để trống")]
    [StringLength(72)]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "Mật khẩu mới từ 8 đến 72 ký tự, tối đa 72 byte UTF-8")]
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateProfileRequestDto
{
    [Required(ErrorMessage = "Họ và tên không được để trống")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    [StringLength(300)] public string? Address { get; set; }

    // Doctor profile fields
    [StringLength(50)] public string? Title { get; set; }
    [StringLength(1000)] public string? Bio { get; set; }
}

public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Patient info
    public Guid? PatientId { get; set; }
    public string? PatientCode { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }

    // Doctor info
    public Guid? DoctorId { get; set; }
    public string? DoctorCode { get; set; }
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Specialties { get; set; } = new();
}
