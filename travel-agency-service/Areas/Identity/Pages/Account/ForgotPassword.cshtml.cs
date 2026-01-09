using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.ComponentModel.DataAnnotations;
using travel_agency_service.Models;
using System.Text;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordModel(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }


    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email);

        // 🔒 אבטחה – לא חושפים אם המשתמש לא קיים
        if (user == null)
        {
            return RedirectToPage("./ForgotPasswordConfirmation");
        }


        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(
            System.Text.Encoding.UTF8.GetBytes(code)
        );

        var callbackUrl = Url.Page(
    "/Account/ResetPassword",
    pageHandler: null,
    values: new { code = code, email = Input.Email },
    protocol: Request.Scheme);


        await _emailSender.SendEmailAsync(
            Input.Email,
            "Reset your password – Travel Agency",
            $@"
Hello,

To reset your password, click the link below:

<a href='{callbackUrl}'>Reset Password</a>

If you did not request this, please ignore this email.
");

        return RedirectToPage("./ForgotPasswordConfirmation");
    }
}
