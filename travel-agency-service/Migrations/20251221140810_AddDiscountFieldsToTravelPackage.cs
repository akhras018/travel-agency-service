using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace travel_agency_service.Migrations
{
    public partial class AddDiscountFieldsToTravelPackage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "TravelPackages",
                newName: "BasePrice");

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountEnd",
                table: "TravelPackages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPrice",
                table: "TravelPackages",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountStart",
                table: "TravelPackages",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountEnd",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "DiscountPrice",
                table: "TravelPackages");

            migrationBuilder.DropColumn(
                name: "DiscountStart",
                table: "TravelPackages");

            migrationBuilder.RenameColumn(
                name: "BasePrice",
                table: "TravelPackages",
                newName: "Price");
        }
    }
}
