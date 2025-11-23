using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMSBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class PendingChangesFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AutorId",
                table: "Posts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Foros",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_AutorId",
                table: "Posts",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Hilos_AutorId",
                table: "Hilos",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Foros_CreadorId",
                table: "Foros",
                column: "CreadorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Foros_AspNetUsers_CreadorId",
                table: "Foros",
                column: "CreadorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Hilos_AspNetUsers_AutorId",
                table: "Hilos",
                column: "AutorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_AspNetUsers_AutorId",
                table: "Posts",
                column: "AutorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Foros_AspNetUsers_CreadorId",
                table: "Foros");

            migrationBuilder.DropForeignKey(
                name: "FK_Hilos_AspNetUsers_AutorId",
                table: "Hilos");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_AspNetUsers_AutorId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_AutorId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Hilos_AutorId",
                table: "Hilos");

            migrationBuilder.DropIndex(
                name: "IX_Foros_CreadorId",
                table: "Foros");

            migrationBuilder.AlterColumn<string>(
                name: "AutorId",
                table: "Posts",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Foros",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
