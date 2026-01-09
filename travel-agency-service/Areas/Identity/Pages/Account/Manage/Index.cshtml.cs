// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using travel_agency_service.Models;

namespace travel_agency_service.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // 🔹 Read-only display
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            FirstName = user.FirstName;
            LastName = user.LastName;

            Input = new InputModel
            {
                Email = user.Email
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // ✅ Allow changing ONLY email
            if (Input.Email != user.Email)
            {
                var result = await _userManager.SetEmailAsync(user, Input.Email);
                if (!result.Succeeded)
                {
                    StatusMessage = "Error updating email.";
                    return RedirectToPage();
                }

                await _signInManager.RefreshSignInAsync(user);
            }

            StatusMessage = "Your profile has been updated.";
            return RedirectToPage();
        }
    }
}
