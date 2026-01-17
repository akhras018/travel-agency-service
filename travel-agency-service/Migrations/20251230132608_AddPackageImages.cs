using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace travel_agency_service.Migrations
{
    public partial class AddPackageImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrls",
                table: "TravelPackages",
                newName: "MainImageUrl");

            migrationBuilder.AddColumn<string>(
                name: "GalleryImagesJson",
                table: "TravelPackages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GalleryImagesJson",
                table: "TravelPackages");

            migrationBuilder.RenameColumn(
                name: "MainImageUrl",
                table: "TravelPackages",
                newName: "ImageUrls");
        }
    }
}
