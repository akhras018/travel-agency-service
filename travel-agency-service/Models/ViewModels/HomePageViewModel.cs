using System.Collections.Generic;
using travel_agency_service.Models;

namespace travel_agency_service.Models.ViewModels
{
    public class HomePageViewModel
    {
        // Hero stats
        public int TripsCount { get; set; }
        public int DestinationsCount { get; set; }
        public int BookingsCount { get; set; }

        // Site reviews
        public List<SiteReview> SiteReviews { get; set; } = new();
    }
}
