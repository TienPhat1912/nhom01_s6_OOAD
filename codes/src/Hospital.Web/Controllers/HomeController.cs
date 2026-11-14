using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Hospital.Web.Models;

namespace Hospital.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Dashboard") : View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View("ApiError", new UiMessage("Đã xảy ra lỗi", "Vui lòng thử lại. Nếu lỗi tiếp tục, cung cấp mã hỗ trợ cho người phụ trách.", "error", Activity.Current?.Id ?? HttpContext.TraceIdentifier));
    }
}
