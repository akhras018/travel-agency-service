using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace travel_agency_service.Models
{
    public class WaitingListEntry
    {
        public int Id { get; set; }

        // 🔗 Package
        [Required]
        public int TravelPackageId { get; set; }
        public TravelPackage TravelPackage { get; set; }

        // 👤 User
        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // ⏱ FIFO order
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 📧 Safety flag (email sent)
        public bool Notified { get; set; } = false;

        public DateTime? NotificationSentAt { get; set; }

    }
}
