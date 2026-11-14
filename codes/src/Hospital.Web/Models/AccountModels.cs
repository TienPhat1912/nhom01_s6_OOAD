using System.ComponentModel.DataAnnotations;
using Hospital.Application.DTOs.Auth;

namespace Hospital.Web.Models;

public class RegisterModel : RegisterRequestDto
{
    [Required, Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận chưa khớp.")]
    public string ConfirmPassword { get; set; } = "";
}

public class ChangePasswordModel : ChangePasswordRequestDto
{
    [Required, Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận chưa khớp.")]
    public string ConfirmPassword { get; set; } = "";
}
