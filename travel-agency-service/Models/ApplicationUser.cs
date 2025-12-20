using Microsoft.AspNetCore.Identity;

namespace travel_agency_service.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
