using System;

namespace travel_agency_service.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int PackageId { get; set; }
        public int Id { get; set; }            // ✅ זה החסר

        public string Destination { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> RoomTypes { get; set; } = new();

        public int Rooms { get; set; }

        public decimal UnitPrice { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public decimal TotalPrice => UnitPrice * Rooms;
    }
}
