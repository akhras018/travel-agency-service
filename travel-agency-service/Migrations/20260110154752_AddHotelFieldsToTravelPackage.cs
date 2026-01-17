using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace travel_agency_service.Migrations
{
    public partial class AddHotelFieldsToTravelPackage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HotelMeals",
                table: "TravelPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotelName",
                table: "TravelPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotelWebsite",
                table: "TravelPackages",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HotelMeals",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "HotelName",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "HotelWebsite",
                table: "TravelPackages");
        }
    }
}
