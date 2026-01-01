namespace travel_agency_service.Models.ViewModels
{
    public class TripGalleryItemViewModel
    {
        public TravelPackage Package { get; set; }

        public int BookingCount { get; set; }

        public int WaitingCount { get; set; }

        public bool IsUserWaiting { get; set; }

        public int? UserPosition { get; set; }
        public int ReviewsCount { get; set; }
        public List<TripReview> LatestReviews { get; set; } = new();

    }
}
