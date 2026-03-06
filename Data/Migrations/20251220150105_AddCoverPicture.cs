using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProiectDotNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverPicture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverPicture",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverPicture",
                table: "AspNetUsers");
        }
    }
}
