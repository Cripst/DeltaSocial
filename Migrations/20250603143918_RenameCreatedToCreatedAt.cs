using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeltaSocial.Migrations
{
    /// <inheritdoc />
    public partial class RenameCreatedToCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Created",
                table: "Posts",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Posts");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Posts",
                newName: "Created");
        }
    }
}
