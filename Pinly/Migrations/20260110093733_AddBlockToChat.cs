using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinly.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockToChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "GroupMemberships",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "GroupMemberships");
        }
    }
}
