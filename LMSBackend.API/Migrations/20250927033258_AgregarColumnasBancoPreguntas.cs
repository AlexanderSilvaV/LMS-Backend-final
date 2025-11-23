using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LMSBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarColumnasBancoPreguntas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BancoPreguntas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Enunciado = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DocenteId = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Puntos = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BancoPreguntas", x => x.Id);
                    table.CheckConstraint("CK_BancoPregunta_Puntos", "\"Puntos\" > 0 AND \"Puntos\" <= 100");
                    table.ForeignKey(
                        name: "FK_BancoPreguntas_AspNetUsers_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpcionesBanco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Texto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EsCorrecta = table.Column<bool>(type: "boolean", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    BancoPreguntaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcionesBanco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpcionesBanco_BancoPreguntas_BancoPreguntaId",
                        column: x => x.BancoPreguntaId,
                        principalTable: "BancoPreguntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BancoPregunta_Categoria",
                table: "BancoPreguntas",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_BancoPregunta_Docente_Activa",
                table: "BancoPreguntas",
                columns: new[] { "DocenteId", "Activa" });

            migrationBuilder.CreateIndex(
                name: "IX_OpcionesBanco_BancoPreguntaId",
                table: "OpcionesBanco",
                column: "BancoPreguntaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpcionesBanco");

            migrationBuilder.DropTable(
                name: "BancoPreguntas");
        }
    }
}
