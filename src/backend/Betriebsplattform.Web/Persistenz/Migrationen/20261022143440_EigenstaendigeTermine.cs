using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betriebsplattform.Web.Persistenz.Migrationen
{
    /// <inheritdoc />
    public partial class EigenstaendigeTermine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Termine",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EigentuemerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Titel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Beschreibung = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Ort = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Beginn = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Ende = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Ganztaegig = table.Column<bool>(type: "boolean", nullable: false),
                    BeginnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Entfernt = table.Column<bool>(type: "boolean", nullable: false),
                    AnlageHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Termine", x => x.Id);
                    table.CheckConstraint("Termin_Titel", "length(trim(\"Titel\")) > 0");
                    table.CheckConstraint("Termin_Zeit", "\"EndeUtc\" > \"BeginnUtc\"");
                    table.CheckConstraint("Termine_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Termine_Benutzer_EigentuemerId",
                        column: x => x.EigentuemerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Terminverlauf",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TerminId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BenutzerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Zeitpunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Vorher = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    Nachher = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terminverlauf", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Terminverlauf_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Terminverlauf_Termine_TerminId",
                        column: x => x.TerminId,
                        principalTable: "Termine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Termine_EigentuemerId_Entfernt_Beginn",
                table: "Termine",
                columns: new[] { "EigentuemerId", "Entfernt", "Beginn" });

            migrationBuilder.CreateIndex(
                name: "IX_Terminverlauf_BenutzerId",
                table: "Terminverlauf",
                column: "BenutzerId");

            migrationBuilder.CreateIndex(
                name: "IX_Terminverlauf_TerminId_Zeitpunkt",
                table: "Terminverlauf",
                columns: new[] { "TerminId", "Zeitpunkt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Terminverlauf");

            migrationBuilder.DropTable(
                name: "Termine");
        }
    }
}
