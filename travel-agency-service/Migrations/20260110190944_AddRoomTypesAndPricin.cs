using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace travel_agency_service.Migrations
{
    public partial class AddRoomTypesAndPricin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeluxeRoomExtra",
                table: "TravelPackages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardRoomExtra",
                table: "TravelPackages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SuiteRoomExtra",
                table: "TravelPackages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RoomTypesJson",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeluxeRoomExtra",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "StandardRoomExtra",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "SuiteRoomExtra",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "RoomTypesJson",
                table: "Bookings");
        }
    }
}
