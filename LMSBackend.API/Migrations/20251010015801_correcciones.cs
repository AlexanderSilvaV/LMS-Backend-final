using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LMSBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class Correcciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoJob = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorreosEnviados = table.Column<int>(type: "integer", nullable: false),
                    CorreosFallidos = table.Column<int>(type: "integer", nullable: false),
                    IntentosEjecucion = table.Column<int>(type: "integer", nullable: false),
                    MensajeError = table.Column<string>(type: "text", nullable: true),
                    DetallesEjecucion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutionLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutionLog_FechaInicio",
                table: "JobExecutionLogs",
                column: "FechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutionLog_Tipo_Estado",
                table: "JobExecutionLogs",
                columns: new[] { "TipoJob", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobExecutionLogs");
        }
    }
}
