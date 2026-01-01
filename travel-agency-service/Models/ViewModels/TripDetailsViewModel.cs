namespace travel_agency_service.Models.ViewModels
{
    public class TripDetailsViewModel
    {
        public TravelPackage Package { get; set; } = null!;

        public int AvailableRooms { get; set; }
        public List<string> GalleryImages { get; set; } = new();

        public int Rooms { get; set; } = 1;
        public int Adults { get; set; } = 1;
        public int Children { get; set; } = 0;

        public List<int> ChildrenAges { get; set; } = new();

        public decimal TotalPrice { get; set; }

        public bool CanBook { get; set; }
        public bool IsFull { get; set; }

        public int WaitingCount { get; set; } 
        public int? UserWaitingPosition { get; set; }
        public DateTime? EstimatedAvailableDate { get; set; }
        public bool IsUserWaiting { get; set; }
        public bool AlreadyBooked { get; set; }
        public bool CanReview { get; set; }
        public List<TripReview> Reviews { get; set; } = new();


    }
}
