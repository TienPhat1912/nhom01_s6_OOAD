using Hospital.Application.Common.Interfaces;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// UC01 — Đăng ký tài khoản Bệnh nhân
    /// </summary>
    [HttpPost("register"), EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(ApiResponse<AuthResponseDto>.Succeeded(result, "Đăng ký tài khoản thành công!"));
    }

    /// <summary>
    /// UC02 — Đăng nhập hệ thống (Bệnh nhân, Bác sĩ, Nhân viên, Quản trị viên)
    /// </summary>
    [HttpPost("login"), EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponseDto>.Succeeded(result, "Đăng nhập thành công!"));
    }

    /// <summary>
    /// UC03 — Lấy thông tin cá nhân của người dùng hiện tại
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(ApiResponse.Failed("Phiên đăng nhập không hợp lệ hoặc đã hết hạn."));
        }

        var profile = await _authService.GetProfileAsync(_currentUserService.UserId.Value);
        return Ok(ApiResponse<UserProfileDto>.Succeeded(profile));
    }

    /// <summary>
    /// UC03 — Cập nhật thông tin cá nhân
    /// </summary>
    [Authorize]
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(ApiResponse.Failed("Phiên đăng nhập không hợp lệ hoặc đã hết hạn."));
        }

        var profile = await _authService.UpdateProfileAsync(_currentUserService.UserId.Value, request);
        return Ok(ApiResponse<UserProfileDto>.Succeeded(profile, "Cập nhật thông tin cá nhân thành công!"));
    }

    /// <summary>
    /// UC04 — Đổi mật khẩu
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(ApiResponse.Failed("Phiên đăng nhập không hợp lệ hoặc đã hết hạn."));
        }

        await _authService.ChangePasswordAsync(_currentUserService.UserId.Value, request);
        return Ok(ApiResponse.Ok("Đổi mật khẩu thành công!"));
    }
}
