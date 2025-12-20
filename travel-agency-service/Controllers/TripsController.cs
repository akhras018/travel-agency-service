using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace travel_agency_service.Controllers
{
    [Authorize] // 👈 רק משתמשים מחוברים
    public class TripsController : Controller
    {
        public IActionResult Gallery()
        {
            return View();
        }
    }
}
