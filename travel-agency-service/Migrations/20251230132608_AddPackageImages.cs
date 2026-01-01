using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace travel_agency_service.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageImages : Migration
    {
        /// <inheritdoc />
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

        /// <inheritdoc />
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
