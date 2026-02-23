using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayurveda_chatBot.Migrations
{
    /// <inheritdoc />
    public partial class isOnboardingCompletedadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isOnboardingCompleted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isOnboardingCompleted",
                table: "Users");
        }
    }
}
