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

            IQueryable<TravelPackage> packagesQuery =
                _context.TravelPackages.Where(p => p.IsVisible);

            if (category.HasValue)
                packagesQuery = packagesQuery.Where(p => p.PackageType == category.Value);

            if (!string.IsNullOrWhiteSpace(destination))
                packagesQuery = packagesQuery.Where(p => p.Destination.Contains(destination));

            if (!string.IsNullOrWhiteSpace(country))
                packagesQuery = packagesQuery.Where(p => p.Country.Contains(country));

            if (onlyDiscounted)
            {
                packagesQuery = packagesQuery.Where(p =>
                    p.DiscountPrice.HasValue &&
                    p.DiscountStart <= now &&
                    p.DiscountEnd >= now);
            }

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

            var waitingLists = await _context.WaitingListEntries
                .OrderBy(w => w.CreatedAt)
                .ToListAsync();

            var bookingCounts = await _context.Bookings
                .GroupBy(b => b.TravelPackageId)
                .Select(g => new { PackageId = g.Key, Count = g.Count() })
                .ToListAsync();

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
                .AnyAsync(w => w.TravelPackageId == packageId && w.UserId == userId);

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

            // ❌ בדיקה: הזמנה כפולה לאותו Trip
            var alreadyBooked = await _context.Bookings
                .AnyAsync(b =>
                    b.UserId == userId &&
                    b.TravelPackageId == packageId);

            if (alreadyBooked)
            {
                TempData["Message"] = "You already booked this trip.";
                return RedirectToAction(nameof(Gallery));
            }

            // ✅ בדיקה: עד 3 הזמנות עתידיות בלבד
            var activeBookingsCount = await _context.Bookings
                .Include(b => b.TravelPackage)
                .CountAsync(b =>
                    b.UserId == userId &&
                    b.TravelPackage.StartDate > DateTime.UtcNow);

            if (activeBookingsCount >= 3)
            {
                TempData["Message"] = "You can have up to 3 upcoming bookings only.";
                return RedirectToAction(nameof(Gallery));
            }

            // הסרה מרשימת המתנה אם קיים
            var waitingEntries = await _context.WaitingListEntries
                .Where(w => w.TravelPackageId == packageId && w.UserId == userId)
                .ToListAsync();

            if (waitingEntries.Any())
                _context.WaitingListEntries.RemoveRange(waitingEntries);

            package.AvailableRooms -= 1;
            _context.TravelPackages.Update(package);

            _context.Bookings.Add(new Booking
            {
                TravelPackageId = packageId,
                UserId = userId
            });

            await _context.SaveChangesAsync();

            await _waitingListService.NotifyNextUserIfRoomAvailable(packageId);

            TempData["Message"] = "Booking successful!";
            return RedirectToAction(nameof(Gallery));
        }

        // =========================
        // GET: Trips/MyBookings
        // =========================
        public async Task<IActionResult> MyBookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var bookings = await _context.Bookings
                .Include(b => b.TravelPackage)
                .Where(b => b.UserId == userId)
                .OrderBy(b => b.TravelPackage.StartDate)
                .ToListAsync();

            return View(bookings);
        }

        // =========================
        // POST: Cancel Booking
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = await _context.Bookings
                .Include(b => b.TravelPackage)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                return NotFound();

            booking.TravelPackage.AvailableRooms += 1;
            _context.TravelPackages.Update(booking.TravelPackage);

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            await _waitingListService.NotifyNextUserIfRoomAvailable(booking.TravelPackageId);

            TempData["Message"] = "Booking canceled successfully.";
            return RedirectToAction("MyBookings");
        }

        // =========================
        // GET: Trips/Pay
        // =========================
        [HttpGet]
        public async Task<IActionResult> Pay(int bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                return NotFound();

            if (booking.IsPaid)
            {
                TempData["Message"] = "This booking is already paid.";
                return RedirectToAction(nameof(MyBookings));
            }

            ViewBag.BookingId = bookingId;
            return View();
        }

        [HttpGet]


        // =========================
        // POST: Trips/ConfirmPayment
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(
    int bookingId,
    string cardHolderName,
    string cardNumber,
    int expMonth,
    int expYear,
    string cvv)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                return NotFound();

            // ❌ בדיקה: שדות ריקים
            if (string.IsNullOrWhiteSpace(cardHolderName) ||
                string.IsNullOrWhiteSpace(cardNumber) ||
                string.IsNullOrWhiteSpace(cvv))
            {
                TempData["Error"] = "Please fill all payment fields.";
                return RedirectToAction(nameof(Pay), new { bookingId });
            }

            // ❌ מספר כרטיס לא תקין
            if (cardNumber.Length != 16 || !cardNumber.All(char.IsDigit))
            {
                TempData["Error"] = "Invalid card number.";
                return RedirectToAction(nameof(Pay), new { bookingId });
            }

            // ❌ CVV לא תקין
            if (cvv.Length != 3 || !cvv.All(char.IsDigit))
            {
                TempData["Error"] = "Invalid CVV code.";
                return RedirectToAction(nameof(Pay), new { bookingId });
            }

            // ❌ כרטיס שפג תוקף
            var expirationDate = new DateTime(expYear, expMonth, 1).AddMonths(1).AddDays(-1);
            if (expirationDate < DateTime.Today)
            {
                TempData["Error"] = "Card has expired.";
                return RedirectToAction(nameof(Pay), new { bookingId });
            }

            // ❌ כישלון אקראי (סימולציית בנק)
            var random = new Random();
            if (random.Next(1, 5) == 1) // ~25% כישלון
            {
                TempData["Error"] = "Payment was declined by the bank.";
                return RedirectToAction(nameof(Pay), new { bookingId });
            }

            // ✅ הצלחה
            booking.IsPaid = true;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Payment completed successfully.";
            return RedirectToAction(nameof(PaymentSuccess));

        }


        public IActionResult PaymentSuccess()
        {
            return View();
        }





    }
}
