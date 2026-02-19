using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ayurveda_chatBot.Migrations
{
    /// <inheritdoc />
    public partial class updatepassinuser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatHistories_Users_UserId",
                table: "ChatHistories");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ChatHistories",
                newName: "ChatSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatHistories_UserId",
                table: "ChatHistories",
                newName: "IX_ChatHistories_ChatSessionId");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserId",
                table: "ChatSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatHistories_ChatSessions_ChatSessionId",
                table: "ChatHistories",
                column: "ChatSessionId",
                principalTable: "ChatSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatHistories_ChatSessions_ChatSessionId",
                table: "ChatHistories");

            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.RenameColumn(
                name: "ChatSessionId",
                table: "ChatHistories",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatHistories_ChatSessionId",
                table: "ChatHistories",
                newName: "IX_ChatHistories_UserId");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatHistories_Users_UserId",
                table: "ChatHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
