using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using travel_agency_service.Models;
using System.IO;

namespace travel_agency_service.Pdf
{
    public class BookingItineraryPdf : IDocument
    {
        private readonly Booking _booking;
        private readonly byte[] _logo;


        public BookingItineraryPdf(Booking booking)
        {
            _booking = booking;
            _logo = File.ReadAllBytes("wwwroot/images/logoo.png");

        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Element(container =>
                {
                    container.Row(row =>
                    {
                        // טקסט משמאל
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Travel Agency Service")
                                .FontSize(18)
                                .Bold();

                            col.Item().Text("Booking Itinerary")
                                .FontSize(12)
                                .FontColor(Colors.Grey.Darken1);
                        });

                        // תמונה מימין
                        row.ConstantItem(150)
                            .AlignRight()
                            .AlignMiddle()
                            .Height(90)                // ⬅️ גובה הלוגו
                            .Image(_logo, ImageScaling.FitArea);
                    });
                });
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text("Travel Agency Service © 2025");
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Travel Itinerary")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Medium);

                    col.Item().Text("Booking Confirmation")
                        .FontSize(14)
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(100).AlignRight().Height(60)
                    .Placeholder(); // 🔵 כאן תוכל לשים לוגו בהמשך
            });
        }

        private void ComposeContent(IContainer container)
        {
            var p = _booking.TravelPackage;

            container.Column(col =>
            {
                col.Spacing(15);

                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                col.Item().Text($"Destination: {p.Destination}, {p.Country}")
                    .FontSize(16).Bold();

                col.Item().Text($"Travel Dates: {p.StartDate:d} – {p.EndDate:d}");

                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                col.Item().Text("Booking Details")
                    .FontSize(18)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(150);
                        columns.RelativeColumn();
                    });

                    void Row(string label, string value)
                    {
                        table.Cell().Text(label).Bold();
                        table.Cell().Text(value);
                    }

                    var p = _booking.TravelPackage;
                    var unitPrice = p.GetCurrentPrice();
                    var totalPrice = unitPrice * _booking.Rooms;

                    Row("Booking ID", _booking.Id.ToString());
                    Row("Booked By", _booking.User.Email);
                    Row("Destination", $"{p.Destination}, {p.Country}");
                    Row("Travel Dates", $"{p.StartDate:d} – {p.EndDate:d}");
                    Row("Rooms", _booking.Rooms.ToString());
                    if (_booking.RoomTypes.Any())
                    {
                        var roomTypesText = string.Join("\n",
                            _booking.RoomTypes.Select((rt, i) => $"Room {i + 1}: {rt}"));

                        Row("Room Types", roomTypesText);
                    }
                    Row("Price per Room", $"₪{unitPrice}");
                    Row("Total Price", $"₪{totalPrice}");  
                    Row("Payment Status", _booking.IsPaid ? "Paid" : "Unpaid");
                });


                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                col.Item().Text("Cancellation Policy")
                    .FontSize(16).Bold();

                col.Item().Text(
                    p.CancellationDeadline.HasValue
                        ? $"Free cancellation until {p.CancellationDeadline.Value:d}"
                        : "No cancellation allowed for this trip."
                );
            });
        }
    }
}
