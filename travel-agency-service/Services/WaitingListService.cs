using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using travel_agency_service.Data;

namespace travel_agency_service.Services
{
    public class WaitingListService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public WaitingListService(
            ApplicationDbContext context,
            IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task NotifyNextUserIfRoomAvailable(int packageId)
        {
            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package == null || package.AvailableRooms <= 0)
                return;

            var now = DateTime.UtcNow;
            var expiration = TimeSpan.FromHours(24); // או דקה לבדיקה

            var waitingList = await _context.WaitingListEntries
                .Include(w => w.User)
                .Where(w => w.TravelPackageId == packageId)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync();

            if (!waitingList.Any())
                return;

            // מחיקת משתמשים שפג להם הזמן
            var expired = waitingList
                .Where(w =>
                    w.NotificationSentAt != null &&
                    now - w.NotificationSentAt.Value > expiration)
                .ToList();

            if (expired.Any())
            {
                _context.WaitingListEntries.RemoveRange(expired);
                await _context.SaveChangesAsync();
                waitingList = waitingList.Except(expired).ToList();
            }

            if (!waitingList.Any())
                return;

            var nextUser = waitingList.First();

            if (nextUser.NotificationSentAt != null)
                return;

            await _emailSender.SendEmailAsync(
                nextUser.User.Email,
                "A room is now available!",
                $"Hello {nextUser.User.FirstName}, a room is now available."
            );

            nextUser.NotificationSentAt = now;
            await _context.SaveChangesAsync();
        }
    }
}
