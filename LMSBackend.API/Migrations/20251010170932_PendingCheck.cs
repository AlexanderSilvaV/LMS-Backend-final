using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LMSBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class PendingCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropTable(
                name: "JobExecutionLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Asunto = table.Column<string>(type: "text", nullable: false),
                    Destinatario = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    EvaluacionId = table.Column<int>(type: "integer", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HiloId = table.Column<int>(type: "integer", nullable: true),
                    IntentosEnvio = table.Column<int>(type: "integer", nullable: false),
                    MensajeError = table.Column<string>(type: "text", nullable: true),
                    PostId = table.Column<int>(type: "integer", nullable: true),
                    SendGridMessageId = table.Column<string>(type: "text", nullable: true),
                    TipoNotificacion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CorreosEnviados = table.Column<int>(type: "integer", nullable: false),
                    CorreosFallidos = table.Column<int>(type: "integer", nullable: false),
                    DetallesEjecucion = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IntentosEjecucion = table.Column<int>(type: "integer", nullable: false),
                    MensajeError = table.Column<string>(type: "text", nullable: true),
                    TipoJob = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutionLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLog_Destinatario",
                table: "EmailLogs",
                column: "Destinatario");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLog_Estado_Fecha",
                table: "EmailLogs",
                columns: new[] { "Estado", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutionLog_FechaInicio",
                table: "JobExecutionLogs",
                column: "FechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutionLog_Tipo_Estado",
                table: "JobExecutionLogs",
                columns: new[] { "TipoJob", "Estado" });
        }
    }
}
