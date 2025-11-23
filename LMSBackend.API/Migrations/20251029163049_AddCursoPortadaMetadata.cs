using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMSBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCursoPortadaMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PortadaActualizada",
                table: "Cursos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortadaFileId",
                table: "Cursos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortadaUrl",
                table: "Cursos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PortadaActualizada",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "PortadaFileId",
                table: "Cursos");

            migrationBuilder.DropColumn(
                name: "PortadaUrl",
                table: "Cursos");
        }
    }
}
