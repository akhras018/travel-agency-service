using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace travel_agency_service.Migrations
{
    public partial class AddBookingAndCancellationRules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationDeadline",
                table: "TravelPackages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBookingDate",
                table: "TravelPackages",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationDeadline",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "LastBookingDate",
                table: "TravelPackages");
        }
    }
}
