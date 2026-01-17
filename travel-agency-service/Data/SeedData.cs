using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using travel_agency_service.Models;

namespace travel_agency_service.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager =
                serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var admins = new[]
            {
                new { Email = "admin1@travel.com", Password = "Admin123!" },
                new { Email = "admin2@travel.com", Password = "Admin123!" }
            };

            foreach (var admin in admins)
            {
                var user = await userManager.FindByEmailAsync(admin.Email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = admin.Email,
                        Email = admin.Email,
                        EmailConfirmed = true,
                        FirstName = "Admin",
                        LastName = "System"
                    };

                    await userManager.CreateAsync(user, admin.Password);
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }
    }
}
