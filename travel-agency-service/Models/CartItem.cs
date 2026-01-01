namespace travel_agency_service.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int TravelPackageId { get; set; }
        public TravelPackage TravelPackage { get; set; }

        public int Rooms { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
