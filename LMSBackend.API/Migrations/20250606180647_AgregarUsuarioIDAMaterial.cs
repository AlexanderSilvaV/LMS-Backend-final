using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMSBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarUsuarioIDAMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioId",
                table: "Materiales",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Materiales_UsuarioId",
                table: "Materiales",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materiales_AspNetUsers_UsuarioId",
                table: "Materiales",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materiales_AspNetUsers_UsuarioId",
                table: "Materiales");

            migrationBuilder.DropIndex(
                name: "IX_Materiales_UsuarioId",
                table: "Materiales");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Materiales");
        }
    }
}
