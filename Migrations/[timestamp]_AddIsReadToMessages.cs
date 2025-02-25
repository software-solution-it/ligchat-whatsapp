using Microsoft.EntityFrameworkCore.Migrations;

namespace WhatsAppProject.Migrations
{
    public partial class AddIsReadToMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "lido",
                table: "messages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lido",
                table: "messages");
        }
    }
} 