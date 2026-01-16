using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using travel_agency_service.Data;
using travel_agency_service.Models;
using travel_agency_service.Helpers;
using travel_agency_service.Models.ViewModels;
using System.Text.Json;

[Authorize]
public class CartController : Controller
{
    private readonly ApplicationDbContext _context;

    public CartController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var items = await _context.CartItems
            .Include(c => c.TravelPackage)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var model = items.Select(c => new CartItemViewModel
        {
            Id = c.Id,   

            PackageId = c.TravelPackageId,
            Destination = c.TravelPackage.Destination,
            Country = c.TravelPackage.Country,
            Rooms = c.Rooms,
            StartDate = c.TravelPackage.StartDate,
            EndDate = c.TravelPackage.EndDate,
            UnitPrice    = c.TravelPackage.GetCurrentPrice(),
            ImageUrl = c.TravelPackage.MainImageUrl
                ?.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(),
            RoomTypes = string.IsNullOrWhiteSpace(c.RoomTypesJson)
    ? new List<string>()
    : JsonSerializer.Deserialize<List<string>>(c.RoomTypesJson)!

        }).ToList();

        return View(model);
    }


    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int packageId, int rooms, string roomTypesJson)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var existing = await _context.CartItems.FirstOrDefaultAsync(c =>
            c.UserId == userId && c.TravelPackageId == packageId);

        if (existing != null)
        {
            existing.Rooms += rooms;
        }
        else
        {
            _context.CartItems.Add(new CartItem
            {
                UserId = userId,
                TravelPackageId = packageId,
                Rooms = rooms,
                RoomTypesJson = roomTypesJson ?? "[]",
                CreatedAt = DateTime.UtcNow
            });

        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index", "Cart");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var item = await _context.CartItems
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

        if (item != null)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var cartItems = await _context.CartItems
            .Include(c => c.TravelPackage)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (!cartItems.Any())
            return RedirectToAction(nameof(Index));

        foreach (var item in cartItems)
        {
            if (item.TravelPackage.AvailableRooms < item.Rooms)
                continue;

            item.TravelPackage.AvailableRooms -= item.Rooms;

            _context.Bookings.Add(new Booking
            {
                UserId = userId,
                TravelPackageId = item.TravelPackageId,
                Rooms = item.Rooms,
                IsPaid = false
            });
        }

        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();

        return RedirectToAction("MyBookings", "Trips");
    }

}
