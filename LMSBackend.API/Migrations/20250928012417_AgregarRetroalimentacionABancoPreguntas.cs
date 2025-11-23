using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMSBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRetroalimentacionABancoPreguntas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Enunciado",
                table: "BancoPreguntas",
                newName: "Texto");

            migrationBuilder.AddColumn<string>(
                name: "AutorId",
                table: "BancoPreguntas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CursoNrc",
                table: "BancoPreguntas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Dificultad",
                table: "BancoPreguntas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacionUtc",
                table: "BancoPreguntas",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ModuloId",
                table: "BancoPreguntas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Retroalimentacion",
                table: "BancoPreguntas",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tema",
                table: "BancoPreguntas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextoNormalizado",
                table: "BancoPreguntas",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutorId",
                table: "BancoPreguntas");

            migrationBuilder.DropColumn(
                name: "CursoNrc",
                table: "BancoPreguntas");

            migrationBuilder.DropColumn(
                name: "Dificultad",
                table: "BancoPreguntas");

            migrationBuilder.DropColumn(
                name: "FechaCreacionUtc",
                table: "BancoPreguntas");

            migrationBuilder.DropColumn(
                name: "ModuloId",
                table: "BancoPreguntas");

            migrationBuilder.DropColumn(
                name: "Retroalimentacion",
                table: "BancoPreguntas");

            migrationBuilder.DropColumn(
                name: "Tema",
                table: "BancoPreguntas");

            migrationBuilder.DropColumn(
                name: "TextoNormalizado",
                table: "BancoPreguntas");

            migrationBuilder.RenameColumn(
                name: "Texto",
                table: "BancoPreguntas",
                newName: "Enunciado");
        }
    }
}
