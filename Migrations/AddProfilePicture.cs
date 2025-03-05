using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubdivisionWebsite.Migrations
{
    /// <summary>
    /// Migration to add ProfilePicture field to AspNetUsers table
    /// </summary>
    public partial class AddProfilePicture : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePicture",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "AspNetUsers");
        }
    }
} 