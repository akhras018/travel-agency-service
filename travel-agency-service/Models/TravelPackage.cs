using System.ComponentModel.DataAnnotations;

namespace travel_agency_service.Models
{
    public class TravelPackage
    {
        public int Id { get; set; }

        [Required]
        public string Destination { get; set; } = "";

        [Required]
        public string Country { get; set; } = "";

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Range(0, 100000)]
        public decimal BasePrice { get; set; }

        [Range(0, 100000)]
        public decimal? DiscountPrice { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DiscountStart { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DiscountEnd { get; set; }

        public int AvailableRooms { get; set; }

        public PackageType PackageType { get; set; }

        public int AgeLimitation { get; set; }

        [Required]  
        public string Description { get; set; } = "";

        public string MainImageUrl { get; set; } = "";     
        public string GalleryImagesJson { get; set; } = "[]";


        public bool HasActiveDiscount()
        {
            var now = DateTime.UtcNow;
            return DiscountPrice.HasValue
                && DiscountStart.HasValue
                && DiscountEnd.HasValue
                && now >= DiscountStart.Value
                && now <= DiscountEnd.Value;
        }

        public decimal GetCurrentPrice()
        {
            return HasActiveDiscount() ? DiscountPrice!.Value : BasePrice;
        }
        public bool IsVisible { get; set; } = true;
        [DataType(DataType.Date)]
        public DateTime? LastBookingDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CancellationDeadline { get; set; }
        public string? HotelName { get; set; }

        public string? HotelMeals { get; set; }

        [Url]
        public string? HotelWebsite { get; set; }
        public decimal StandardRoomExtra { get; set; } = 0;
        public decimal DeluxeRoomExtra { get; set; } = 300;
        public decimal SuiteRoomExtra { get; set; } = 700;

    }

}
