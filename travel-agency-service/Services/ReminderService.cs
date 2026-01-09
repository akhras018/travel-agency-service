using Microsoft.EntityFrameworkCore;
using travel_agency_service.Data;

namespace travel_agency_service.Services
{
    public class ReminderService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailSender _emailSender;

        public ReminderService(
            ApplicationDbContext context,
            EmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // 🔔 Send reminders 5 days before trip
        public async Task SendUpcomingTripRemindersAsync()

        {
            Console.WriteLine("🔔 ReminderService RUNNING");

            var today = DateTime.Today;

            var bookings = await _context.Bookings
                .Include(b => b.TravelPackage)
                .Include(b => b.User)
                .ToListAsync();

            foreach (var booking in bookings)
            {
                var package = booking.TravelPackage;
                if (package == null)
                    continue;

                int daysBeforeTrip =
                    (package.StartDate.Date - today).Days;

                // ⏰ RULE DEFINED BY ADMIN: 5 days before trip
                if (daysBeforeTrip ==5)

                {
                    await _emailSender.SendEmailAsync(
                        booking.User.Email,
                        "Upcoming Trip Reminder ✈️",
                        $"Dear {booking.User.UserName},<br/><br/>" +
                        $"This is a reminder that your trip to <b>{package.Destination}</b> " +
                        $"starts in 5 days.<br/><br/>Safe travels!"
                    );
                }
            }
        }
    }
}
