using System.ComponentModel.DataAnnotations;

namespace travel_agency_service.Models.ViewModels
{
    public class AddReviewViewModel
    {
        [Required]
        public int TravelPackageId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Comment { get; set; }
    }
}
