using System;
using System.ComponentModel.DataAnnotations;

namespace travel_agency_service.Models
{
    public class SiteReview
    {
        public int Id { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required, MaxLength(500)]
        public string Comment { get; set; }

        public string UserEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;

    }
}
