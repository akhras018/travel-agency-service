using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using travel_agency_service.Data;
using travel_agency_service.Models;
using travel_agency_service.Services;
using travel_agency_service.Models.ViewModels;

namespace travel_agency_service.Controllers
{
    [Authorize]
    public class TripsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly WaitingListService _waitingListService;

        public TripsController(
            ApplicationDbContext context,
            WaitingListService waitingListService)
        {
            _context = context;
            _waitingListService = waitingListService;
        }

        // =========================
        // GET: Trips/Gallery
        // =========================
        public async Task<IActionResult> Gallery(
            string? sortBy,
            PackageType? category,
            string? destination,
            string? country,
            decimal? minPrice,
            decimal? maxPrice,
            bool onlyDiscounted = false)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var now = DateTime.UtcNow;

            // 🔹 בסיס הקטלוג – רק חבילות גלויות
            IQueryable<TravelPackage> packagesQuery =
                _context.TravelPackages.Where(p => p.IsVisible);

            // 🔍 סינון לפי קטגוריה
            if (category.HasValue)
            {
                packagesQuery = packagesQuery
                    .Where(p => p.PackageType == category.Value);
            }

            // 🔍 סינון לפי יעד
            if (!string.IsNullOrWhiteSpace(destination))
            {
                packagesQuery = packagesQuery
                    .Where(p => p.Destination.Contains(destination));
            }

            // 🔍 סינון לפי מדינה
            if (!string.IsNullOrWhiteSpace(country))
            {
                packagesQuery = packagesQuery
                    .Where(p => p.Country.Contains(country));
            }

            // 🔻 סינון מבצעים בלבד
            if (onlyDiscounted)
            {
                packagesQuery = packagesQuery
                    .Where(p =>
                        p.DiscountPrice.HasValue &&
                        p.DiscountStart <= now &&
                        p.DiscountEnd >= now);
            }

            // 💰 סינון לפי טווח מחירים
            if (minPrice.HasValue)
            {
                packagesQuery = packagesQuery.Where(p =>
                    (p.DiscountPrice.HasValue &&
                     p.DiscountStart <= now &&
                     p.DiscountEnd >= now
                        ? p.DiscountPrice.Value
                        : p.BasePrice) >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                packagesQuery = packagesQuery.Where(p =>
                    (p.DiscountPrice.HasValue &&
                     p.DiscountStart <= now &&
                     p.DiscountEnd >= now
                        ? p.DiscountPrice.Value
                        : p.BasePrice) <= maxPrice.Value);
            }

            // 🔃 מיון
            if (sortBy == "popular")
            {
                packagesQuery =
                    from p in packagesQuery
                    join b in _context.Bookings
                        on p.Id equals b.TravelPackageId into bookingGroup
                    orderby bookingGroup.Count() descending
                    select p;
            }
            else
            {
                packagesQuery = sortBy switch
                {
                    "price_asc" => packagesQuery.OrderBy(p =>
                        p.DiscountPrice.HasValue &&
                        p.DiscountStart <= now &&
                        p.DiscountEnd >= now
                            ? p.DiscountPrice.Value
                            : p.BasePrice),

                    "price_desc" => packagesQuery.OrderByDescending(p =>
                        p.DiscountPrice.HasValue &&
                        p.DiscountStart <= now &&
                        p.DiscountEnd >= now
                            ? p.DiscountPrice.Value
                            : p.BasePrice),

                    "date" => packagesQuery.OrderBy(p => p.StartDate),

                    _ => packagesQuery
                };
            }

            var packages = await packagesQuery.ToListAsync();

            // ⏳ Waiting lists
            var waitingLists = await _context.WaitingListEntries
                .OrderBy(w => w.CreatedAt)
                .ToListAsync();

            // ⭐ חישוב פופולריות
            var bookingCounts = await _context.Bookings
                .GroupBy(b => b.TravelPackageId)
                .Select(g => new
                {
                    PackageId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // 🧠 בניית ViewModel
            var model = packages.Select(p =>
            {
                var waitingForPackage = waitingLists
                    .Where(w => w.TravelPackageId == p.Id)
                    .ToList();

                var userEntry = waitingForPackage
                    .FirstOrDefault(w => w.UserId == currentUserId);

                var bookingCount = bookingCounts
                    .FirstOrDefault(b => b.PackageId == p.Id)?.Count ?? 0;

                return new TripGalleryItemViewModel
                {
                    Package = p,
                    BookingCount = bookingCount,
                    WaitingCount = waitingForPackage.Count(w => w.UserId != currentUserId),
                    IsUserWaiting = userEntry != null,
                    UserPosition = userEntry != null
                        ? waitingForPackage.IndexOf(userEntry) + 1
                        : (int?)null
                };
            }).ToList();

            // 📌 שמירת מצב UI
            ViewBag.SortBy = sortBy;
            ViewBag.Category = category;
            ViewBag.OnlyDiscounted = onlyDiscounted;

            return View(model);
        }

        // =========================
        // POST: Join Waiting List
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinWaitingList(int packageId)
        {
            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package == null)
                return NotFound();

            if (package.AvailableRooms > 0)
            {
                TempData["Message"] = "Rooms are available. You can book directly.";
                return RedirectToAction(nameof(Gallery));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var alreadyWaiting = await _context.WaitingListEntries
                .AnyAsync(w =>
                    w.TravelPackageId == packageId &&
                    w.UserId == userId);

            if (alreadyWaiting)
            {
                TempData["Message"] = "You are already on the waiting list.";
                return RedirectToAction(nameof(Gallery));
            }

            _context.WaitingListEntries.Add(new WaitingListEntry
            {
                TravelPackageId = packageId,
                UserId = userId
            });

            await _context.SaveChangesAsync();

            TempData["Message"] = "You have been added to the waiting list.";
            return RedirectToAction(nameof(Gallery));
        }

        // =========================
        // POST: Book
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int packageId)
        {
            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package == null)
                return NotFound();

            if (package.AvailableRooms <= 0)
            {
                TempData["Message"] = "No rooms available.";
                return RedirectToAction(nameof(Gallery));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🔴 הסרת המשתמש מרשימת המתנה אם קיים
            var waitingEntries = await _context.WaitingListEntries
                .Where(w =>
                    w.TravelPackageId == packageId &&
                    w.UserId == userId)
                .ToListAsync();

            if (waitingEntries.Any())
            {
                _context.WaitingListEntries.RemoveRange(waitingEntries);
            }

            // 🏨 הקטנת מספר החדרים
            package.AvailableRooms -= 1;
            _context.TravelPackages.Update(package);

            // ⭐ יצירת Booking (זה מה שסופר פופולריות)
            _context.Bookings.Add(new Booking
            {
                TravelPackageId = packageId,
                UserId = userId,
            });

            await _context.SaveChangesAsync();


            // 🔔 שליחת מייל למשתמש הבא אם נשאר חדר
            await _waitingListService.NotifyNextUserIfRoomAvailable(packageId);

            TempData["Message"] = "Booking successful!";
            return RedirectToAction(nameof(Gallery));
        }
    }
}
