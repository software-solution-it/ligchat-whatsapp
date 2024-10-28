using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsAppProject.Migrations
{
    /// <inheritdoc />
    public partial class AddContactTableAndChanges2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "whatsapp_credentials");

            migrationBuilder.DropColumn(
                name: "credential_id",
                table: "contacts");

            migrationBuilder.RenameColumn(
                name: "sent_at",
                table: "messages",
                newName: "data_envio");

            migrationBuilder.RenameColumn(
                name: "receiver_id",
                table: "messages",
                newName: "id_destinatario");

            migrationBuilder.RenameColumn(
                name: "media_url",
                table: "messages",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "media_type",
                table: "messages",
                newName: "tipo");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "messages",
                newName: "conteudo");

            migrationBuilder.RenameColumn(
                name: "sender_id",
                table: "messages",
                newName: "id_setor");

            migrationBuilder.RenameColumn(
                name: "sector_id",
                table: "contacts",
                newName: "setor_id");

            migrationBuilder.RenameColumn(
                name: "profile_picture_url",
                table: "contacts",
                newName: "foto_perfil");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "contacts",
                newName: "numero");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "contacts",
                newName: "nome");

            migrationBuilder.CreateTable(
                name: "sector",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descricao = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    numero = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    token_acesso = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_criacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sector", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sector");

            migrationBuilder.RenameColumn(
                name: "url",
                table: "messages",
                newName: "media_url");

            migrationBuilder.RenameColumn(
                name: "tipo",
                table: "messages",
                newName: "media_type");

            migrationBuilder.RenameColumn(
                name: "id_destinatario",
                table: "messages",
                newName: "receiver_id");

            migrationBuilder.RenameColumn(
                name: "data_envio",
                table: "messages",
                newName: "sent_at");

            migrationBuilder.RenameColumn(
                name: "conteudo",
                table: "messages",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "id_setor",
                table: "messages",
                newName: "sender_id");

            migrationBuilder.RenameColumn(
                name: "setor_id",
                table: "contacts",
                newName: "sector_id");

            migrationBuilder.RenameColumn(
                name: "numero",
                table: "contacts",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "contacts",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "foto_perfil",
                table: "contacts",
                newName: "profile_picture_url");

            migrationBuilder.AddColumn<int>(
                name: "credential_id",
                table: "contacts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "whatsapp_credentials",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    access_token = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    phone_number_id = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sector_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_credentials", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
