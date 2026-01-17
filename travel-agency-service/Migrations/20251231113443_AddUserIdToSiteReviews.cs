using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace travel_agency_service.Migrations
{
    public partial class AddUserIdToSiteReviews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "SiteReviews",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SiteReviews_UserId",
                table: "SiteReviews",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteReviews_AspNetUsers_UserId",
                table: "SiteReviews",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SiteReviews_AspNetUsers_UserId",
                table: "SiteReviews");

            migrationBuilder.DropIndex(
                name: "IX_SiteReviews_UserId",
                table: "SiteReviews");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SiteReviews");
        }
    }
}
