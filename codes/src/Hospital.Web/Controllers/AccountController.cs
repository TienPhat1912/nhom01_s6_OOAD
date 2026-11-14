using System.Security.Claims;
using Hospital.Application.DTOs.Auth;
using Hospital.Client;
using Hospital.Web.Models;
using Hospital.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

public class AccountController(HospitalApiClient api, PortalApi portal) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl) => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Dashboard") : View(new LoginModel { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var auth = await api.SendAsync<AuthResponseDto>(HttpMethod.Post, "api/auth/login",
                new LoginRequestDto { Username = model.Username, Password = model.Password }, cancellationToken: HttpContext.RequestAborted);
            await SignIn(auth);
            return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction("Index", "Dashboard");
        }
        catch (HospitalApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewData["TraceId"] = ex.TraceId;
            model.Password = "";
            ModelState.Remove(nameof(model.Password));
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Register() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Dashboard") : View(new RegisterModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        if (ModelState.IsValid)
        {
            try
            {
                var auth = await api.SendAsync<AuthResponseDto>(HttpMethod.Post, "api/auth/register", new RegisterRequestDto
                {
                    Username = model.Username, Password = model.Password, FullName = model.FullName, Email = model.Email,
                    PhoneNumber = model.PhoneNumber, DateOfBirth = model.DateOfBirth, Gender = model.Gender, Address = model.Address
                }, cancellationToken: HttpContext.RequestAborted);
                await SignIn(auth);
                TempData["Success"] = "Tài khoản bệnh nhân đã được tạo. Bạn có thể đặt lịch khám ngay.";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (HospitalApiException ex) { ModelState.AddModelError("", ex.Message); ViewData["TraceId"] = ex.TraceId; }
        }
        model.Password = model.ConfirmPassword = "";
        return View(model);
    }

    [Authorize, HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordModel());

    [Authorize, HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await portal.SendAsync<object>(HttpMethod.Post, "api/auth/change-password",
                    new ChangePasswordRequestDto { OldPassword = model.OldPassword, NewPassword = model.NewPassword });
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["Success"] = "Mật khẩu đã được thay đổi. Vui lòng đăng nhập lại bằng mật khẩu mới.";
                return RedirectToAction(nameof(Login));
            }
            catch (HospitalApiException ex) { ModelState.AddModelError("", ex.Message); ViewData["TraceId"] = ex.TraceId; }
        }
        // Preserve validation messages while never echoing secrets into the response.
        model.OldPassword = model.NewPassword = model.ConfirmPassword = "";
        return View(model);
    }

    private Task SignIn(AuthResponseDto auth)
    {
        var identity = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, auth.UserId.ToString()), new Claim(ClaimTypes.Name, auth.FullName),
            new Claim(ClaimTypes.Role, auth.Role), new Claim("access_token", auth.Token)
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { ExpiresUtc = new DateTimeOffset(DateTime.SpecifyKind(auth.ExpiresAt, DateTimeKind.Utc)), IsPersistent = false });
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> Profile()
    {
        var p = await portal.GetAsync<UserProfileDto>("api/auth/profile");
        return View(new ProfileModel { FullName = p.FullName, Email = p.Email, PhoneNumber = p.PhoneNumber,
            DateOfBirth = p.DateOfBirth, Gender = p.Gender, Address = p.Address, Title = p.Title, Bio = p.Bio,
            Role = p.Role, Username = p.Username });
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Profile(ProfileModel model)
    {
        // Role and username shown in the form always come from the backend, never form input.
        var current = await portal.GetAsync<UserProfileDto>("api/auth/profile");
        model.Role = current.Role; model.Username = current.Username;
        if (!ModelState.IsValid) return View(model);
        try
        {
            await portal.SendAsync<object>(HttpMethod.Put, "api/auth/profile", new UpdateProfileRequestDto
            { FullName = model.FullName, Email = model.Email, PhoneNumber = model.PhoneNumber, DateOfBirth = model.DateOfBirth,
              Gender = model.Gender, Address = model.Address, Title = model.Title, Bio = model.Bio });
            TempData["Success"] = "Thông tin cá nhân đã được cập nhật.";
            return RedirectToAction(nameof(Profile));
        }
        catch (HospitalApiException ex)
        { ModelState.AddModelError("", ex.Message); ViewData["TraceId"] = ex.TraceId; return View(model); }
    }

    public IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        return View("ApiError", new UiMessage("Bạn không có quyền truy cập", "Chọn một chức năng trong menu phù hợp với tài khoản của bạn.", "error"));
    }
}
