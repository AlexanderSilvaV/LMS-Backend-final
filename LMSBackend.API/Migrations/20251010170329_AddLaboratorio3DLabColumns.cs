using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LMSBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLaboratorio3DLabColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsLaboratorio3DLab",
                table: "Evaluaciones",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PreguntasMinimasLaboratorio",
                table: "Evaluaciones",
                type: "integer",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<bool>(
                name: "EsLaboratorio",
                table: "Notas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "SesionesEvaluacion",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsLaboratorio3DLab",
                table: "Evaluaciones");

            migrationBuilder.DropColumn(
                name: "PreguntasMinimasLaboratorio",
                table: "Evaluaciones");

            migrationBuilder.DropColumn(
                name: "EsLaboratorio",
                table: "Notas");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "SesionesEvaluacion");
        }
    }
}
