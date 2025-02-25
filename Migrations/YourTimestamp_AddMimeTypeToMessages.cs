using Microsoft.EntityFrameworkCore.Migrations;

namespace WhatsAppProject.Migrations
{
    public partial class AddMimeTypeToMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mime_type",
                table: "messages",
                type: "varchar(100)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mime_type",
                table: "messages");
        }
    }
} 