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

    // ✅ הוספה בלבד – Constructor
    public AdminController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    // ====== קיים ======
    public IActionResult Index()
    {
        return View();
    }

    // ====== קיים ======
    public IActionResult Packages()
    {
        return RedirectToAction("Index", "TravelPackages");
    }


    // ====== קיים (תוכן עודכן – חתימה לא שונתה) ======
    public IActionResult Users()
    {
        var users = _userManager.Users.ToList();
        return View(users);
    }

    // ====== חדש – היסטוריית הזמנות למשתמש ======
    public IActionResult UserBookings(string id)
    {
        var bookings = _context.Bookings
            .Include(b => b.TravelPackage)
            .Where(b => b.UserId == id)
            .ToList();

        return View(bookings);
    }

    // ====== חדש – חסימה / שחרור משתמש ======
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
        {
            // Unlock
            user.LockoutEnd = null;
        }
        else
        {
            // Lock
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