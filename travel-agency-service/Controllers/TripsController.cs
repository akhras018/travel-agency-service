using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Security.Claims;
using travel_agency_service.Data;
using travel_agency_service.Models;
using travel_agency_service.Models.ViewModels;
using travel_agency_service.Pdf;
using travel_agency_service.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace travel_agency_service.Controllers
{
    [Authorize]
    public class TripsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly WaitingListService _waitingListService;
        private readonly IEmailSender _emailSender;

        public TripsController(
            ApplicationDbContext context,
            WaitingListService waitingListService, IEmailSender emailSender)
        {
            _context = context;
            _waitingListService = waitingListService;
            _emailSender = emailSender;
        }
        [AllowAnonymous]

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
            var reviewsLookup = await _context.TripReviews
    .GroupBy(r => r.TravelPackageId)
    .Select(g => new
    {
        PackageId = g.Key,
        Reviews = g.ToList()
    })
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

                var latestReviews = reviewsLookup
     .FirstOrDefault(r => r.PackageId == p.Id)?.Reviews
     ?? new List<TripReview>();

                return new TripGalleryItemViewModel
                {
                    Package = p,
                    BookingCount = bookingCount,
                    WaitingCount = waitingForPackage.Count(w => w.UserId != currentUserId),
                    IsUserWaiting = userEntry != null,
                    UserPosition = userEntry != null
                        ? waitingForPackage.IndexOf(userEntry) + 1
                        : (int?)null,

                    LatestReviews = latestReviews
                };

            }).ToList();

            ViewBag.SortBy = sortBy;
            ViewBag.Category = category;
            ViewBag.OnlyDiscounted = onlyDiscounted;
            var siteReviews = await _context.SiteReviews
           .OrderByDescending(r => r.CreatedAt)
            .Take(6)
          .ToListAsync();

            ViewBag.SiteReviews = siteReviews;


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
            bool canReview = false;

            if (User.Identity!.IsAuthenticated)
            {
                canReview = await _context.Bookings
                    .Include(b => b.TravelPackage)
                    .AnyAsync(b =>
                        b.UserId == userId &&
                        b.TravelPackageId == package.Id &&
                        b.IsPaid &&
                        b.TravelPackage.StartDate < DateTime.UtcNow
                    );
            }

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
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });


            await _context.SaveChangesAsync();

            TempData["Message"] = "You have been added to the waiting list.";
            return RedirectToAction(nameof(Gallery));
        }

        // =========================
        // POST: Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int packageId, int rooms, bool payNow)

        {


            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ❌ כבר הזמין את הטיול הזה
            var alreadyBooked = await _context.Bookings.AnyAsync(b =>
                b.UserId == userId &&
                b.TravelPackageId == packageId);

            if (alreadyBooked)
            {
                TempData["Message"] = "כבר הזמנת את הטיול הזה. לא ניתן להזמין אותו שוב.";
                return RedirectToAction(nameof(Details), new { id = packageId });
            }
            if (package.LastBookingDate.HasValue &&
             DateTime.UtcNow > package.LastBookingDate.Value)
            {
                TempData["Message"] = "Booking period for this trip has ended.";
                return RedirectToAction(nameof(Details), new { id = package.Id });
            }

            // ❌ יותר מ־3 הזמנות עתידיות
            var activeBookingsCount = await _context.Bookings
                .Include(b => b.TravelPackage)
                .CountAsync(b =>
                    b.UserId == userId &&
                    b.TravelPackage.StartDate > DateTime.UtcNow);

            if (activeBookingsCount >= 3)
            {
                TempData["Message"] = "לא ניתן להזמין יותר מ־3 טיולים עתידיים.";
                return RedirectToAction(nameof(Details), new { id = packageId });
            }

            // ❌ אין מספיק חדרים
            if (rooms <= 0 || package.AvailableRooms < rooms)
            {
                TempData["Message"] = "אין מספיק חדרים פנויים.";
                return RedirectToAction(nameof(Details), new { id = packageId });
            }

            // ✅ הורדת חדרים
            package.AvailableRooms -= rooms;
            _context.TravelPackages.Update(package);


            // יצירת ההזמנה
            // יצירת ההזמנה
            var booking = new Booking
            {
                TravelPackageId = packageId,
                UserId = userId,
                Rooms = rooms
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // 🔀 ניתוב לפי הכפתור
            if (payNow)
            {
                return RedirectToAction(nameof(Pay), new { bookingId = booking.Id });
            }

            return RedirectToAction(nameof(MyBookings));



        }
        [Authorize]
        public async Task<IActionResult> Checkout(int packageId, int rooms)
        {
            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package == null)
                return NotFound();

            ViewBag.PackageId = package.Id;
            ViewBag.Rooms = rooms;
            ViewBag.Price = package.GetCurrentPrice() * rooms;
            ViewBag.PackageName = $"{package.Destination}, {package.Country}";

            return View();
        }



        // =========================
        // GET: Trips/MyBookings
        // =========================
        public async Task<IActionResult> MyBookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var now = DateTime.UtcNow;

            // כל ההזמנות של המשתמש
            var bookings = await _context.Bookings
                .Include(b => b.TravelPackage)
                .Where(b => b.UserId == userId)
                .OrderBy(b => b.TravelPackage.StartDate)
                .ToListAsync();

            // 🔹 טיולים עתידיים / פעילים
            var upcomingBookings = bookings
                .Where(b => b.TravelPackage.EndDate >= now)
                .ToList();

            // 🔹 טיולים שכבר הסתיימו
            var pastBookings = bookings
                .Where(b => b.TravelPackage.EndDate < now)
                .ToList();

            // Reviews של האתר (צד ימין)
            ViewBag.SiteReviews = await _context.SiteReviews
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // העברה ל־View
            ViewBag.UpcomingBookings = upcomingBookings;
            ViewBag.PastBookings = pastBookings;

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

            // ⛔ בדיקת מועד אחרון לביטול
            if (booking.TravelPackage.CancellationDeadline.HasValue &&
                DateTime.UtcNow > booking.TravelPackage.CancellationDeadline.Value)
            {
                TempData["Message"] = "לא ניתן לבטל הזמנה לאחר המועד האחרון לביטול.";
                return RedirectToAction(nameof(MyBookings));
            }



            // ✅ החזרת חדרים לפי כמות
            booking.TravelPackage.AvailableRooms += booking.Rooms;

            _context.TravelPackages.Update(booking.TravelPackage);
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            await _waitingListService.NotifyNextUserIfRoomAvailable(booking.TravelPackageId);

            TempData["Message"] = "ההזמנה בוטלה בהצלחה.";
            return RedirectToAction(nameof(MyBookings));
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
    .Include(b => b.User)
    .Include(b => b.TravelPackage)
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
            var totalPrice =
    booking.TravelPackage.GetCurrentPrice() * booking.Rooms;

            // ✅ הצלחה
            booking.IsPaid = true;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            await _emailSender.SendEmailAsync(
     booking.User.Email,
     "Payment Successful – Travel Agency Service",
 $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
</head>
<body style='margin:0;padding:0;background-color:#f4f6f8;font-family:Arial,Helvetica,sans-serif;'>

<table width='100%' cellpadding='0' cellspacing='0'>
<tr>
<td align='center'>

    <table width='600' cellpadding='0' cellspacing='0' 
           style='background:#ffffff;border-radius:10px;overflow:hidden;
                  box-shadow:0 8px 30px rgba(0,0,0,0.08);'>

        <!-- Header -->
        <tr>
            <td style='background:#0d6efd;color:white;padding:30px;text-align:center;'>
                <h1 style='margin:0;font-size:26px;'>✈️ Travel Agency Service</h1>
                <p style='margin:6px 0 0;font-size:15px;opacity:0.9;'>
                    Booking Confirmation
                </p>
            </td>
        </tr>

        <!-- Body -->
        <tr>
            <td style='padding:30px;color:#333;'>

                <h2 style='margin-top:0;color:#0d6efd;'>
                    Payment Successful ✅
                </h2>

                <p style='font-size:15px;line-height:1.6;'>
                    Hello <strong>{booking.User.Email}</strong>,<br /><br />
                    We are happy to inform you that your payment for the trip to
                    <strong>{booking.TravelPackage.Destination}</strong> has been completed successfully.
                </p>

                <!-- Booking Details -->
                <table width='100%' cellpadding='0' cellspacing='0'
                       style='margin-top:20px;border-collapse:collapse;'>

                    <tr>
                        <td style='padding:10px;border-bottom:1px solid #eee;'><strong>Booking ID</strong></td>
                        <td style='padding:10px;border-bottom:1px solid #eee;'>{booking.Id}</td>
                    </tr>

                    <tr>
                        <td style='padding:10px;border-bottom:1px solid #eee;'><strong>Destination</strong></td>
                        <td style='padding:10px;border-bottom:1px solid #eee;'>{booking.TravelPackage.Destination}</td>
                    </tr>

                    <tr>
                        <td style='padding:10px;border-bottom:1px solid #eee;'><strong>Travel Dates</strong></td>
                        <td style='padding:10px;border-bottom:1px solid #eee;'>
                            {booking.TravelPackage.StartDate:dd/MM/yyyy} –
                            {booking.TravelPackage.EndDate:dd/MM/yyyy}
                        </td>
                    </tr>

                    <tr>
                        <td style='padding:10px;border-bottom:1px solid #eee;'><strong>Rooms</strong></td>
                        <td style='padding:10px;border-bottom:1px solid #eee;'>{booking.Rooms}</td>
                    </tr>
<tr>
    <td style='padding:10px;border-bottom:1px solid #eee;'>
        <strong>Total Paid</strong>
    </td>
    <td style='padding:10px;border-bottom:1px solid #eee;'>
        ₪{totalPrice}
    </td>
</tr>

                    <tr>
                        <td style='padding:10px;'><strong>Payment Status</strong></td>
                        <td style='padding:10px;color:#198754;font-weight:bold;'>Paid</td>
                    </tr>

                </table>

                <!-- CTA -->


                <p style='margin-top:30px;font-size:14px;color:#666;'>
                    Thank you for choosing <strong>Travel Agency Service</strong>.<br />
                    We wish you a wonderful trip 🌍
                </p>

            </td>
        </tr>

        <!-- Footer -->
        <tr>
            <td style='background:#f1f3f5;padding:15px;text-align:center;
                       font-size:12px;color:#777;'>
                © {DateTime.Now.Year} Travel Agency Service. All rights reserved.
            </td>
        </tr>

    </table>

</td>
</tr>
</table>

</body>
</html>
"
 );


            TempData["Message"] = "Payment completed successfully.";

            // 🔁 חזרה לגלריה
            return RedirectToAction("Gallery", "Trips");



        }

        [HttpGet]
        public IActionResult ConfirmPayment()
        {
            return RedirectToAction(nameof(MyBookings));
        }

        public IActionResult PaymentSuccess()
        {
            return View();
        }






        // =========================
        // GET: Trips/Search
        // =========================
        [AllowAnonymous]

        public async Task<IActionResult> Search(
            string? destination,
            DateTime? startDate,
            DateTime? endDate,
            decimal? minPrice,
            decimal? maxPrice,
            PackageType? category,
            string? sortBy)

        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var now = DateTime.UtcNow;

            IQueryable<TravelPackage> query =
                _context.TravelPackages.Where(p => p.IsVisible);

            if (category.HasValue)
                query = query.Where(p => p.PackageType == category.Value);

            if (!string.IsNullOrWhiteSpace(destination))
                query = query.Where(p => p.Destination.Contains(destination));

            if (startDate.HasValue)
            {
                query = query.Where(p => p.StartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.EndDate <= endDate.Value);
            }


            if (minPrice.HasValue)
            {
                query = query.Where(p =>
                    (p.DiscountPrice.HasValue &&
                     p.DiscountStart <= now &&
                     p.DiscountEnd >= now
                        ? p.DiscountPrice.Value
                        : p.BasePrice) >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p =>
                    (p.DiscountPrice.HasValue &&
                     p.DiscountStart <= now &&
                     p.DiscountEnd >= now
                        ? p.DiscountPrice.Value
                        : p.BasePrice) <= maxPrice.Value);
            }

            // מיון
            if (sortBy == "popular")
            {
                query =
                    from p in query
                    join b in _context.Bookings
                        on p.Id equals b.TravelPackageId into bg
                    orderby bg.Count() descending
                    select p;
            }
            else
            {
                query = sortBy switch
                {
                    "price_asc" => query.OrderBy(p =>
                        p.DiscountPrice.HasValue &&
                        p.DiscountStart <= now &&
                        p.DiscountEnd >= now
                            ? p.DiscountPrice.Value
                            : p.BasePrice),

                    "price_desc" => query.OrderByDescending(p =>
                        p.DiscountPrice.HasValue &&
                        p.DiscountStart <= now &&
                        p.DiscountEnd >= now
                            ? p.DiscountPrice.Value
                            : p.BasePrice),

                    "date" => query.OrderBy(p => p.StartDate),
                    _ => query
                };
            }

            var packages = await query.ToListAsync();

            var bookingCounts = await _context.Bookings
                .GroupBy(b => b.TravelPackageId)
                .Select(g => new { PackageId = g.Key, Count = g.Count() })
                .ToListAsync();

            var reviewCounts = await _context.TripReviews
      .GroupBy(r => r.TravelPackageId)
      .Select(g => new
      {
          PackageId = g.Key,
          Count = g.Count()
      })
      .ToListAsync();
            var reviewsLookup = await _context.TripReviews
.OrderByDescending(r => r.CreatedAt)
.GroupBy(r => r.TravelPackageId)
.Select(g => new
{
    PackageId = g.Key,
    Reviews = g.Take(3).ToList()
})
.ToListAsync();

            var model = packages.Select(p =>
            {
                var bookingCount = bookingCounts
                    .FirstOrDefault(b => b.PackageId == p.Id)?.Count ?? 0;

                var latestReviews = reviewsLookup
                    .FirstOrDefault(r => r.PackageId == p.Id)?.Reviews
                    ?? new List<TripReview>();

                return new TripGalleryItemViewModel
                {
                    Package = p,
                    BookingCount = bookingCount,
                    LatestReviews = latestReviews
                };
            }).ToList();



            // בשביל למלא חזרה את הטופס
            ViewBag.SortBy = sortBy;
            ViewBag.Category = category;
            ViewBag.Destination = destination;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;



            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> TripDetails(int id)
        {
            var trip = await _context.TravelPackages
                .FirstOrDefaultAsync(p => p.Id == id && p.IsVisible);

            if (trip == null)
                return NotFound();

            return PartialView("_TripDetailsPartial", trip);
        }

        [HttpGet]
        [AllowAnonymous]

        public async Task<IActionResult> Details(int id)
        {
            var package = await _context.TravelPackages
                .FirstOrDefaultAsync(p => p.Id == id && p.IsVisible);

            if (package == null)
                return NotFound();

            var waitingList = await _context.WaitingListEntries
                .Where(w => w.TravelPackageId == id)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool canReview = User.Identity!.IsAuthenticated;


            // ❗ בדיקה: האם המשתמש כבר הזמין את הטיול
            var alreadyBooked = await _context.Bookings.AnyAsync(b =>
                b.UserId == userId &&
                b.TravelPackageId == package.Id);
            var userEntry = waitingList.FirstOrDefault(w => w.UserId == userId);
            int waitingCountWithoutUser = waitingList
             .Count(w => w.UserId != userId);

            // ⏱️ הערכת זמן – דוגמה פשוטה:
            // נניח שכל ביטול / שחרור חדר = 2 ימים
            DateTime? estimatedDate = null;
            if (userEntry != null)
            {
                var position = waitingList.IndexOf(userEntry);
                estimatedDate = DateTime.Today.AddDays((position + 1) * 2);
            }
            var reviews = await _context.TripReviews
            .Include(r => r.User)
            .Where(r => r.TravelPackageId == package.Id)
           .OrderByDescending(r => r.CreatedAt)
           .ToListAsync();

            var model = new TripDetailsViewModel
            {
                Package = package,
                AvailableRooms = package.AvailableRooms,
                CanReview = canReview,
                Reviews = reviews,

                GalleryImages = string.IsNullOrWhiteSpace(package.GalleryImagesJson)
                    ? new List<string>()
                    : package.GalleryImagesJson.Split('\n').ToList(),

                IsFull = package.AvailableRooms == 0,
                AlreadyBooked = alreadyBooked,

                WaitingCount = waitingCountWithoutUser,
                IsUserWaiting = userEntry != null,
                UserWaitingPosition = userEntry != null
                    ? waitingList.IndexOf(userEntry) + 1
                    : null,
                EstimatedAvailableDate = estimatedDate
            };



            return View(model);
        }


        [Authorize]
        public IActionResult DownloadItinerary(int bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = _context.Bookings
                .Include(b => b.TravelPackage)
                .Include(b => b.User)
                .FirstOrDefault(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                return Unauthorized(); // 🔒 מונע גישה לזרים

            var pdf = new BookingItineraryPdf(booking).GeneratePdf();

            return File(pdf, "application/pdf", "Itinerary.pdf");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(AddReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Details", new { id = model.TravelPackageId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var canReview = await _context.Bookings
    .Include(b => b.TravelPackage)
    .AnyAsync(b =>
        b.TravelPackageId == model.TravelPackageId &&
        b.UserId == userId &&
        b.IsPaid &&
        b.TravelPackage.StartDate < DateTime.UtcNow
    );

            if (!canReview)
            {
                TempData["Error"] = "You can review this trip only after it has started.";
                return RedirectToAction("Details", new { id = model.TravelPackageId });
            }



            var review = new TripReview
            {
                TravelPackageId = model.TravelPackageId,
                UserId = userId,
                Rating = model.Rating,
                Comment = model.Comment
            };

            _context.TripReviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = model.TravelPackageId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSiteReview(int Rating, string Comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            // הגנה: רק משתמש עם הזמנה



            // אופציונלי: רק Review אחד למשתמש


            var review = new SiteReview
            {
                UserId = userId,
                Rating = Rating,
                Comment = Comment,
                UserEmail = userEmail!,   // ⭐ זה החלק החסר

                CreatedAt = DateTime.UtcNow
            };

            _context.SiteReviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Thank you for your feedback!";
            return RedirectToAction("MyBookings");
        }


      
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CancelPendingBooking(int bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = await _context.Bookings
                .Include(b => b.TravelPackage)
                .FirstOrDefaultAsync(b =>
                    b.Id == bookingId &&
                    b.UserId == userId &&
                    !b.IsPaid);

            if (booking == null)
                return NotFound();

            // 🔁 מחזירים חדרים
            booking.TravelPackage.AvailableRooms += booking.Rooms;

            _context.TravelPackages.Update(booking.TravelPackage);
            _context.Bookings.Remove(booking);

            await _context.SaveChangesAsync();

            TempData["Message"] = "Payment cancelled. Booking was removed.";

            return RedirectToAction(
                "Details",
                "Trips",
                new { id = booking.TravelPackageId }
            );
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SendTripReminders(int daysBefore = 5)
        {
            var targetDate = DateTime.UtcNow.Date.AddDays(daysBefore);

            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TravelPackage)
                .Where(b =>
                    b.IsPaid &&
                    !b.ReminderSent &&
                    b.TravelPackage.StartDate.Date == targetDate
                )
                .ToListAsync();

            foreach (var booking in bookings)
            {
                await _emailSender.SendEmailAsync(
                    booking.User.Email,
                    "✈️ Trip Reminder – Travel Agency Service",
                    $@"
Hello {booking.User.Email},

This is a reminder that your trip to
{booking.TravelPackage.Destination}, {booking.TravelPackage.Country}
will start in {daysBefore} days.

📅 Departure date: {booking.TravelPackage.StartDate:dd/MM/yyyy}

We wish you a wonderful trip!
"
                );

                booking.ReminderSent = true;
            }

            await _context.SaveChangesAsync();

            TempData["Message"] =
                $"Trip reminders sent successfully ({bookings.Count} emails).";

            return RedirectToAction("Dashboard");
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Dashboard()
        {
            return View();
        }


    }

}
