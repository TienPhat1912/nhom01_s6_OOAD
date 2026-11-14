using Microsoft.AspNetCore.Mvc;

namespace Hospital.Web.Controllers;

public class DesignController(IWebHostEnvironment environment) : Controller
{
    public IActionResult Index() => environment.IsDevelopment() ? View() : NotFound();
}
