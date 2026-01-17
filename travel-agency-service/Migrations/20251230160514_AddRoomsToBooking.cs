using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace travel_agency_service.Migrations
{
    public partial class AddRoomsToBooking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rooms",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rooms",
                table: "Bookings");
        }
    }
}
