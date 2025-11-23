using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LMSBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaNota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Calificacion = table.Column<decimal>(type: "numeric(3,1)", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    EvaluacionId = table.Column<int>(type: "integer", nullable: false),
                    FechaCalificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NumeroIntento = table.Column<int>(type: "integer", nullable: false),
                    EsNotaFinal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notas", x => x.Id);
                    table.CheckConstraint("CK_Nota_Calificacion", "\"Calificacion\" >= 1.0 AND \"Calificacion\" <= 7.0");
                    table.ForeignKey(
                        name: "FK_Notas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notas_Evaluaciones_EvaluacionId",
                        column: x => x.EvaluacionId,
                        principalTable: "Evaluaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nota_Usuario_Evaluacion_Intento",
                table: "Notas",
                columns: new[] { "UsuarioId", "EvaluacionId", "NumeroIntento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notas_EvaluacionId",
                table: "Notas",
                column: "EvaluacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notas");
        }
    }
}
