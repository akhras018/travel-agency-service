using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using travel_agency_service.Data;
using travel_agency_service.Models;
using travel_agency_service.Models.ViewModels;

namespace travel_agency_service.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }


        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                return RedirectToAction("Gallery", "Trips");
            }
            var model = new HomePageViewModel
            {
                TripsCount = _context.TravelPackages.Count(p => p.IsVisible),

                DestinationsCount = _context.TravelPackages
                 .Where(p => p.IsVisible)
                 .Select(p => p.Country)
                 .Distinct()
                 .Count(),

                BookingsCount = _context.Bookings.Count(),

                SiteReviews = _context.SiteReviews
                 .Include(r => r.User)
                 .OrderByDescending(r => r.CreatedAt)
                 .Take(6)
                 .ToList()
            };



            return View(model);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
