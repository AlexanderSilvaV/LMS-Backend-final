using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMSBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class AllowLabZeroLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Evaluacion_IntentosMaximos",
                table: "Evaluaciones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Evaluacion_TiempoLimite",
                table: "Evaluaciones");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Evaluacion_IntentosMaximos",
                table: "Evaluaciones",
                sql: "((\"EsLaboratorio3DLab\" = TRUE AND \"IntentosMaximos\" = 0) OR (\"EsLaboratorio3DLab\" = FALSE AND \"IntentosMaximos\" > 0 AND \"IntentosMaximos\" <= 10))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Evaluacion_TiempoLimite",
                table: "Evaluaciones",
                sql: "((\"EsLaboratorio3DLab\" = TRUE AND \"TiempoLimiteMins\" = 0) OR (\"EsLaboratorio3DLab\" = FALSE AND \"TiempoLimiteMins\" > 0 AND \"TiempoLimiteMins\" <= 300))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Evaluacion_IntentosMaximos",
                table: "Evaluaciones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Evaluacion_TiempoLimite",
                table: "Evaluaciones");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Evaluacion_IntentosMaximos",
                table: "Evaluaciones",
                sql: "\"IntentosMaximos\" > 0 AND \"IntentosMaximos\" <= 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Evaluacion_TiempoLimite",
                table: "Evaluaciones",
                sql: "\"TiempoLimiteMins\" > 0 AND \"TiempoLimiteMins\" <= 300");
        }
    }
}
