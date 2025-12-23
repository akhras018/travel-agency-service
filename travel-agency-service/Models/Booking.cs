using System.ComponentModel.DataAnnotations;

namespace travel_agency_service.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public int TravelPackageId { get; set; }
        public TravelPackage TravelPackage { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime BookedAt { get; set; } = DateTime.UtcNow;
    }
}
