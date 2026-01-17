using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace travel_agency_service.Migrations
{
    public partial class AddNotificationSentAtToWaitingList : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NotificationSentAt",
                table: "WaitingListEntries",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationSentAt",
                table: "WaitingListEntries");
        }
    }
}
