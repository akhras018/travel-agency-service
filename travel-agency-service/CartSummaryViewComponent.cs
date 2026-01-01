using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using travel_agency_service.Data;

public class CartSummaryViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public CartSummaryViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!User.Identity.IsAuthenticated)
            return View(0);

        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        var count = await _context.CartItems
    .CountAsync(c => c.UserId == userId);


        return View(count);
    }
}
