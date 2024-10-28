using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsAppProject.Migrations
{
    /// <inheritdoc />
    public partial class AddContactTableAndChanges23 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id_destinatario",
                table: "messages",
                newName: "contato_id");

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "messages",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "tipo",
                table: "messages",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<DateTime>(
                name: "data_envio",
                table: "messages",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.CreateIndex(
                name: "IX_messages_contato_id",
                table: "messages",
                column: "contato_id");

            migrationBuilder.AddForeignKey(
                name: "FK_messages_contacts_contato_id",
                table: "messages",
                column: "contato_id",
                principalTable: "contacts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_messages_contacts_contato_id",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_messages_contato_id",
                table: "messages");

            migrationBuilder.RenameColumn(
                name: "contato_id",
                table: "messages",
                newName: "id_destinatario");

            migrationBuilder.UpdateData(
                table: "messages",
                keyColumn: "url",
                keyValue: null,
                column: "url",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "messages",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "messages",
                keyColumn: "tipo",
                keyValue: null,
                column: "tipo",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "tipo",
                table: "messages",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<DateTime>(
                name: "data_envio",
                table: "messages",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);
        }
    }
}
