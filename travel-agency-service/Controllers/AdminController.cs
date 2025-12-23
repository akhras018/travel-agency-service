using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Packages()
    {
        return View();
    }

    public IActionResult Users()
    {
        return View();
    }
}
