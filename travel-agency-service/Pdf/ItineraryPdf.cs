using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using travel_agency_service.Models;

public class ItineraryPdf : IDocument
{
    private readonly Booking _booking;

    public ItineraryPdf(Booking booking)
    {
        _booking = booking;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var package = _booking.TravelPackage;

        container.Page(page =>
        {
            page.Margin(40);

            page.Header()
                .Text("Travel Itinerary")
                .FontSize(24)
                .Bold()
                .AlignCenter();

            page.Content().Column(column =>
            {
                column.Spacing(10);

                column.Item().Text($"Destination: {package.Destination}, {package.Country}");
                column.Item().Text($"Dates: {package.StartDate:dd/MM/yyyy} - {package.EndDate:dd/MM/yyyy}");
                column.Item().Text($"Package Type: {package.PackageType}");
                column.Item().Text($"Rooms: {_booking.Rooms}");
                column.Item().Text($"Price per person: ₪{package.GetCurrentPrice()}");

                column.Item().PaddingTop(20)
                    .Text("Thank you for booking with Travel Agency Service!")
                    .Italic();
            });

            page.Footer()
                .AlignCenter()
                .Text(text =>
                {
                    text.Span("Generated on ");
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy"));
                });
        });
    }
}
