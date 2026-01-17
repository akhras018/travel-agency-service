using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using travel_agency_service.Data;

namespace travel_agency_service.Services
{
    public class ReminderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;   

        public ReminderService(
            ApplicationDbContext context,
            IEmailSender emailSender)   
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task SendUpcomingTripRemindersAsync()
        {
            Console.WriteLine("🔔 ReminderService RUNNING");

            var today = DateTime.Today;

            var bookings = await _context.Bookings
                .Include(b => b.TravelPackage)
                .Include(b => b.User)
                .ToListAsync();

           
        }
    }
}
