using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMSBackend.API.Migrations
{
    public partial class RemoveAvatarUrlAndStorageIdFromPerfiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Elimina columnas no usadas en la tabla Perfiles
            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Perfiles");

            migrationBuilder.DropColumn(
                name: "AvatarStorageId",
                table: "Perfiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reversión: vuelve a crear las columnas con los mismos tipos/tamaños que tenían
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Perfiles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarStorageId",
                table: "Perfiles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }
    }
}
