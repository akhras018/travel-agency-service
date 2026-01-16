using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using travel_agency_service.Data;
using travel_agency_service.Models;
using System.IO;

namespace travel_agency_service.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TravelPackagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TravelPackagesController(
            ApplicationDbContext context,
            IEmailSender emailSender, IHttpContextAccessor httpContextAccessor)

        {
            _context = context;
            _emailSender = emailSender;
            _httpContextAccessor = httpContextAccessor;

        }

        // GET: TravelPackages
        public async Task<IActionResult> Index(
        string sortBy,
        string search,
        PackageType? category,
        bool? visibleOnly)
        {
            IQueryable<TravelPackage> query = _context.TravelPackages;
            await SendTripRemindersIfNeeded();

            // 🔍 Search (Destination / Country)
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Destination.Contains(search) ||
                    p.Country.Contains(search));
            }

            // 🏷 Filter by category
            if (category.HasValue)
            {
                query = query.Where(p => p.PackageType == category.Value);
            }

            // 👁 Filter visibility
            if (visibleOnly.HasValue)
            {
                query = query.Where(p => p.IsVisible == visibleOnly.Value);
            }

            // 🔃 Sorting
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.BasePrice),
                "price_desc" => query.OrderByDescending(p => p.BasePrice),
                "date" => query.OrderBy(p => p.StartDate),
                "category" => query.OrderBy(p => p.PackageType),
                "visible" => query.OrderByDescending(p => p.IsVisible),
                _ => query.OrderBy(p => p.Id)
            };

            var packages = await query.ToListAsync();

            // 📊 Stats
            var waitingCounts = await _context.WaitingListEntries
                .GroupBy(w => w.TravelPackageId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);

            var bookingCounts = await _context.Bookings
                .GroupBy(b => b.TravelPackageId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);

            ViewBag.SortBy = sortBy;
            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.VisibleOnly = visibleOnly;
            ViewBag.WaitingCounts = waitingCounts;
            ViewBag.BookingCounts = bookingCounts;

            return View(packages);
        }



        // GET: TravelPackages/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TravelPackages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(
    TravelPackage package,
    IFormFile mainImage,
    List<IFormFile> galleryImages)
        {
            if (!ModelState.IsValid)
                return View(package);

            // תמונה ראשית
            if (mainImage != null)
                package.MainImageUrl = await SaveImage(mainImage);

            // גלריה
            if (galleryImages != null && galleryImages.Any())
            {
                var paths = new List<string>();
                foreach (var img in galleryImages)
                    paths.Add(await SaveImage(img));

                package.GalleryImagesJson = string.Join('\n', paths);
            }

            _context.Add(package);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImage(IFormFile file)
        {
            var uploads = Path.Combine("wwwroot", "uploads");
            Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var path = Path.Combine(uploads, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return "/uploads/" + fileName;
        }

        private async Task SendTripRemindersIfNeeded()
        {
            var today = DateTime.Today;
            var reminderDate = today.AddDays(5);

            var bookingsToRemind = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.TravelPackage)
                .Where(b =>
                    !b.ReminderSent &&
                    b.IsPaid &&
                    b.TravelPackage.StartDate.Date == reminderDate)
                .ToListAsync();

            foreach (var booking in bookingsToRemind)
            {
                await _emailSender.SendEmailAsync(
                    booking.User.Email,
                    "Trip Reminder – 5 days to go ✈️",
                    BuildReminderEmail(booking)
                );

                booking.ReminderSent = true;
            }

            await _context.SaveChangesAsync();
        }
        private string BuildReminderEmail(Booking booking)
        {
            return $@"
    <h2>⏰ Trip Reminder</h2>
                    Hello <strong>{booking.User.FirstName} {booking.User.LastName}</strong>

    <p>This is a reminder that your trip to 
    <strong>{booking.TravelPackage.Destination}</strong>
    starts in <strong>5 days</strong>.</p>

    <ul>
        <li><b>Start date:</b> {booking.TravelPackage.StartDate:dd/MM/yyyy}</li>
        <li><b>End date:</b> {booking.TravelPackage.EndDate:dd/MM/yyyy}</li>
        <li><b>Booking ID:</b> {booking.Id}</li>
    </ul>

    <p>We wish you a wonderful trip ✈️🌍</p>
    <p><b>Travel Agency Service</b></p>
    ";
        }


        // GET: TravelPackages/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var package = await _context.TravelPackages.FindAsync(id);
            if (package == null) return NotFound();
            return View(package);
        }

        // POST: TravelPackages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
     
        public async Task<IActionResult> Edit(
    int id,
    TravelPackage package,
    IFormFile? mainImage,
    List<IFormFile>? galleryImages,
    List<string>? ImagesToDelete,
          string? GalleryOrder)
        {
            if (!ModelState.IsValid)
                return View(package);

            NormalizeDiscountFields(package);

            var existingPackage = await _context.TravelPackages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingPackage == null)
                return NotFound();

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads");

            Directory.CreateDirectory(uploadsFolder);

            // ======================
            // 🖼 תמונה ראשית
            // ======================
            if (mainImage != null && mainImage.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(mainImage.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await mainImage.CopyToAsync(stream);

                package.MainImageUrl = "/uploads/" + fileName;
            }
            else
            {
                package.MainImageUrl = existingPackage.MainImageUrl;
            }

            // ======================
            // 🖼 גלריה
            // ======================
            var gallery = (existingPackage.GalleryImagesJson ?? "")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            // מחיקת תמונות שסומנו
            if (ImagesToDelete != null && ImagesToDelete.Any())
            {
                gallery = gallery
                    .Where(img => !ImagesToDelete.Contains(img))
                    .ToList();
            }

            // הוספת תמונות חדשות
            if (galleryImages != null && galleryImages.Any())
            {
                foreach (var img in galleryImages)
                {
                    if (img.Length == 0) continue;

                    var fileName = Guid.NewGuid() + Path.GetExtension(img.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await img.CopyToAsync(stream);

                    gallery.Add("/uploads/" + fileName);
                }
            }
            if (!string.IsNullOrEmpty(GalleryOrder))
            {
                package.GalleryImagesJson = GalleryOrder;
            }


            package.GalleryImagesJson = string.Join('\n', gallery);

            _context.Update(package);
            await _context.SaveChangesAsync();

            await NotifyNextUserIfRoomAvailable(package.Id);

            return RedirectToAction(nameof(Index));
        }



        // GET: TravelPackages/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var package = await _context.TravelPackages.FindAsync(id);
            if (package == null) return NotFound();
            return View(package);
        }

        // POST: TravelPackages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var package = await _context.TravelPackages.FindAsync(id);
            if (package != null)
            {
                _context.TravelPackages.Remove(package);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // -------------------------
        // Helpers – Discounts
        // -------------------------

        private void ValidateDiscountRules(TravelPackage package)
        {
            var anyDiscountFieldFilled =
                package.DiscountPrice.HasValue ||
                package.DiscountStart.HasValue ||
                package.DiscountEnd.HasValue;

            if (!anyDiscountFieldFilled)
                return;

            if (!package.DiscountPrice.HasValue ||
                !package.DiscountStart.HasValue ||
                !package.DiscountEnd.HasValue)
            {
                ModelState.AddModelError(string.Empty,
                    "To set a discount you must provide Discount Price, Start Date, and End Date.");
                return;
            }

            if (package.DiscountPrice.Value >= package.BasePrice)
            {
                ModelState.AddModelError(nameof(package.DiscountPrice),
                    "Discount price must be lower than base price.");
            }

            if (package.DiscountEnd.Value < package.DiscountStart.Value)
            {
                ModelState.AddModelError(nameof(package.DiscountEnd),
                    "Discount end date must be after discount start date.");
            }

            var duration =
                (package.DiscountEnd.Value.Date - package.DiscountStart.Value.Date).TotalDays;

            if (duration > 7)
            {
                ModelState.AddModelError(string.Empty,
                    "Discount cannot be longer than 7 days.");
            }
        }

        private void NormalizeDiscountFields(TravelPackage package)
        {
            if (!package.DiscountPrice.HasValue)
            {
                package.DiscountStart = null;
                package.DiscountEnd = null;
                return;
            }

            if (!package.DiscountStart.HasValue || !package.DiscountEnd.HasValue)
            {
                package.DiscountPrice = null;
                package.DiscountStart = null;
                package.DiscountEnd = null;
            }
        }

        // -------------------------
        // Helpers – Waiting List Email + Expiration
        // -------------------------

        private async Task NotifyNextUserIfRoomAvailable(int packageId)
        {
            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package == null || package.AvailableRooms <= 0)
                return;

            var now = DateTime.UtcNow;
            var expiration = TimeSpan.FromHours(24); // 🔁 בדיקה (להחזיר ל-24 שעות)
            var request = _httpContextAccessor.HttpContext?.Request;

            var baseUrl = $"{request.Scheme}://{request.Host}";
            var tripUrl = $"{baseUrl}/Trips/Details/{package.Id}";

            var waitingList = await _context.WaitingListEntries
                .Include(w => w.User)
                .Where(w => w.TravelPackageId == packageId)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync();

            if (!waitingList.Any())
                return;

            // 🔴 מחיקת משתמשים שפג להם הזמן
            var expiredUsers = waitingList
                .Where(w =>
                    w.NotificationSentAt != null &&
                    now - w.NotificationSentAt.Value > expiration)
                .ToList();

            if (expiredUsers.Any())
            {
                _context.WaitingListEntries.RemoveRange(expiredUsers);
                await _context.SaveChangesAsync();

                waitingList = waitingList.Except(expiredUsers).ToList();
            }

            if (!waitingList.Any())
                return;

            var nextUser = waitingList.First();

            if (nextUser.NotificationSentAt != null &&
                now - nextUser.NotificationSentAt.Value <= expiration)
                return;

            var subject = "A room is now available!";

            var body = $@"
Hello {nextUser.User.FirstName},

Good news! 🎉  
A room is now available for the trip to {package.Destination}, {package.Country}.

👉 Click the link below to book your trip:
{tripUrl}

⏳ Please note: the room is reserved for you for the next 24 hours.

Best regards,
Travel Agency Team
";

            await _emailSender.SendEmailAsync(
                nextUser.User.Email,
                subject,
                body
            );

            nextUser.NotificationSentAt = now;
            _context.WaitingListEntries.Update(nextUser);
            await _context.SaveChangesAsync();
        }

        // GET: TravelPackages/WaitingList/5
        public async Task<IActionResult> WaitingList(int id)
        {
            // 🔔 זה הטריגר שחסר – בדיקת פקיעת זמן + מעבר תור
            await NotifyNextUserIfRoomAvailable(id);

            var package = await _context.TravelPackages
                .FirstOrDefaultAsync(p => p.Id == id);

            if (package == null)
                return NotFound();

            var waitingUsers = await _context.WaitingListEntries
                .Include(w => w.User)
                .Where(w => w.TravelPackageId == id)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync();

            ViewBag.Package = package;
            return View(waitingUsers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var package = await _context.TravelPackages.FindAsync(id);
            if (package == null)
                return NotFound();

            package.IsVisible = !package.IsVisible;
            _context.Update(package);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Dashboard()
        {
            return View();
        }

    }
}
