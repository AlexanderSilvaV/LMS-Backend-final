using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LMSBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class CrearTablaCategorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoriaId",
                table: "BancoPreguntas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DocenteId = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categorias_AspNetUsers_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BancoPreguntas_CategoriaId",
                table: "BancoPreguntas",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_DocenteId",
                table: "Categorias",
                column: "DocenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_BancoPreguntas_Categorias_CategoriaId",
                table: "BancoPreguntas",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BancoPreguntas_Categorias_CategoriaId",
                table: "BancoPreguntas");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_BancoPreguntas_CategoriaId",
                table: "BancoPreguntas");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "BancoPreguntas");
        }
    }
}
