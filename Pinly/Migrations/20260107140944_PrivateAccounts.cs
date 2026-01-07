using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinly.Migrations
{
    /// <inheritdoc />
    public partial class PrivateAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAccepted",
                table: "Follows",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAccepted",
                table: "Follows");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "AspNetUsers");
        }
    }
}
