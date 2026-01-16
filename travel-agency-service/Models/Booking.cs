using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

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
        public bool IsPaid { get; set; } = false;
        public DateTime? PaidAt { get; set; }
        public int Rooms { get; set; }
        public bool ReminderSent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
      
       public string RoomTypesJson { get; set; } = "[]";

        public List<string> RoomTypes =>
            string.IsNullOrWhiteSpace(RoomTypesJson)
                ? new List<string>()
                : (JsonSerializer.Deserialize<List<string>>(RoomTypesJson) ?? new List<string>());

    }
}
