namespace travel_agency_service.Models.ViewModels
{
    public class PayViewModel
    {
        public int BookingId { get; set; }
        public decimal TotalPrice { get; set; }

        public string PublishableKey { get; set; } = "";
        public string ClientSecret { get; set; } = "";

        public string ReturnUrl { get; set; } = "";
    }
}

