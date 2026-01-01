using travel_agency_service.Models;

public class TripReview
{
    public int Id { get; set; }

    public int TravelPackageId { get; set; }
    public TravelPackage TravelPackage { get; set; }

    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public int Rating { get; set; } // 1–5
    public string Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
