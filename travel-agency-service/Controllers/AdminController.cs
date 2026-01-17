using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using travel_agency_service.Models;
using travel_agency_service.Data;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Index", "TravelPackages");
    }


    public IActionResult Packages()
    {
        return RedirectToAction("Index", "TravelPackages");
    }


    public IActionResult Users()
    {
        var users = _userManager.Users.ToList();
        return View(users);
    }

    public IActionResult UserBookings(string id)
    {
        var bookings = _context.Bookings
            .Include(b => b.TravelPackage)
            .Where(b => b.UserId == id)
            .ToList();

        return View(bookings);
    }

    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
        {
            user.LockoutEnd = null;
        }
        else
        {
            user.LockoutEnd = DateTime.UtcNow.AddYears(100);
        }

        await _userManager.UpdateAsync(user);
        return RedirectToAction(nameof(Users));
    }
    public IActionResult WaitingLists()
    {
        var firstPackage = _context.TravelPackages.FirstOrDefault();

        if (firstPackage == null)
            return RedirectToAction("Index", "TravelPackages");

        return RedirectToAction(
            "WaitingList",
            "TravelPackages",
            new { id = firstPackage.Id }
        );
    }
}