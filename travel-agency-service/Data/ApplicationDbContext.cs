using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using travel_agency_service.Models;

namespace travel_agency_service.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TravelPackage> TravelPackages { get; set; }

        // 🕒 Waiting list
        public DbSet<WaitingListEntry> WaitingListEntries { get; set; }

        public DbSet<Booking> Bookings { get; set; }

        public DbSet<SiteReview> SiteReviews { get; set; }

        public DbSet<TripReview> TripReviews { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

    }
}
