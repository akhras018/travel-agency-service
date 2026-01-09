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

        // 💰 Base price (regular price)
        [Range(0, 100000)]
        public decimal BasePrice { get; set; }

        // 🔻 Discount (optional, max 7 days)
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

        // שמירת כל התמונות כמחרוזת (URL בכל שורה)
        // 🖼 תמונה ראשית (Hero / Thumbnail)
        // 📸 תמונה ראשית
        public string MainImageUrl { get; set; } = "";   // תמונה ראשית
        public string GalleryImagesJson { get; set; } = "[]";


        // 🧠 Business logic – active discount check
        public bool HasActiveDiscount()
        {
            var now = DateTime.UtcNow;
            return DiscountPrice.HasValue
                && DiscountStart.HasValue
                && DiscountEnd.HasValue
                && now >= DiscountStart.Value
                && now <= DiscountEnd.Value;
        }

        // 🧮 Current price calculation
        public decimal GetCurrentPrice()
        {
            return HasActiveDiscount() ? DiscountPrice!.Value : BasePrice;
        }
        // 👁 Visibility in catalog (Admin controlled)
        public bool IsVisible { get; set; } = true;
        // ⏳ Admin rules – booking & cancellation
        [DataType(DataType.Date)]
        public DateTime? LastBookingDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CancellationDeadline { get; set; }


    }

}
