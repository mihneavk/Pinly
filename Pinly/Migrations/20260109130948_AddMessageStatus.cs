using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinly.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GroupMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "GroupMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GroupMessages");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "GroupMessages");
        }
    }
}
