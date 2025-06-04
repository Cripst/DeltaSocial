using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeltaSocial.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentApprovalStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Comments");
        }
    }
}
