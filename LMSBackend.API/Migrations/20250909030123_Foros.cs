using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LMSBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class Foros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Foros",
                columns: table => new
                {
                    ForoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false, defaultValue: "Activo"),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModuloId = table.Column<int>(type: "integer", nullable: false),
                    AllowStudentThreads = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RequireInitialPostToView = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreadorId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foros", x => x.ForoId);
                    table.ForeignKey(
                        name: "FK_Foros_Modulos_ModuloId",
                        column: x => x.ModuloId,
                        principalTable: "Modulos",
                        principalColumn: "ModuloId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hilos",
                columns: table => new
                {
                    HiloId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Cerrado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UnlockAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AutorId = table.Column<string>(type: "text", nullable: false),
                    Pinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PinnedOrder = table.Column<int>(type: "integer", nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ForoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hilos", x => x.HiloId);
                    table.ForeignKey(
                        name: "FK_Hilos_Foros_ForoId",
                        column: x => x.ForoId,
                        principalTable: "Foros",
                        principalColumn: "ForoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HiloLecturas",
                columns: table => new
                {
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    HiloId = table.Column<int>(type: "integer", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HiloLecturas", x => new { x.UsuarioId, x.HiloId });
                    table.ForeignKey(
                        name: "FK_HiloLecturas_Hilos_HiloId",
                        column: x => x.HiloId,
                        principalTable: "Hilos",
                        principalColumn: "HiloId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HiloSuscripciones",
                columns: table => new
                {
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    HiloId = table.Column<int>(type: "integer", nullable: false),
                    FechaSuscripcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HiloSuscripciones", x => new { x.UsuarioId, x.HiloId });
                    table.ForeignKey(
                        name: "FK_HiloSuscripciones_Hilos_HiloId",
                        column: x => x.HiloId,
                        principalTable: "Hilos",
                        principalColumn: "HiloId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    PostId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Contenido = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Editado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AutorId = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ParentPostId = table.Column<int>(type: "integer", nullable: true),
                    HiloId = table.Column<int>(type: "integer", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SoftDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.PostId);
                    table.ForeignKey(
                        name: "FK_Posts_Hilos_HiloId",
                        column: x => x.HiloId,
                        principalTable: "Hilos",
                        principalColumn: "HiloId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Foros_Modulo",
                table: "Foros",
                column: "ModuloId");

            migrationBuilder.CreateIndex(
                name: "IX_Foros_Modulo_Estado",
                table: "Foros",
                columns: new[] { "ModuloId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_HiloLecturas_HiloId",
                table: "HiloLecturas",
                column: "HiloId");

            migrationBuilder.CreateIndex(
                name: "IX_Hilos_Foro_Orden",
                table: "Hilos",
                columns: new[] { "ForoId", "Pinned", "PinnedOrder", "LastActivityAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HiloSuscripciones_HiloId",
                table: "HiloSuscripciones",
                column: "HiloId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Hilo_Chrono",
                table: "Posts",
                columns: new[] { "HiloId", "FechaCreacion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HiloLecturas");

            migrationBuilder.DropTable(
                name: "HiloSuscripciones");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Hilos");

            migrationBuilder.DropTable(
                name: "Foros");
        }
    }
}
