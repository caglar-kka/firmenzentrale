using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Betriebsplattform.Web.Persistenz.Migrationen
{
    /// <inheritdoc />
    public partial class Kontaktberechtigungen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Berechtigungsaenderungen",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AkteurId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ZielId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Bereich = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Zeitpunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Vorher = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    Nachher = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Berechtigungsaenderungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Berechtigungsaenderungen_Benutzer_AkteurId",
                        column: x => x.AkteurId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Berechtigungsaenderungen_Benutzer_ZielId",
                        column: x => x.ZielId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rechtevorlagenversionen",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Bereich = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    RechteJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rechtevorlagenversionen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenutzerRechteprofile",
                columns: table => new
                {
                    BenutzerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Bereich = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VorlageId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Modus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AbweichungenJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenutzerRechteprofile", x => new { x.BenutzerId, x.Bereich });
                    table.CheckConstraint("CK_Rechteprofil_Modus", "\"Modus\" IN ('Bestand','Explizit')");
                    table.CheckConstraint("CK_Rechteprofil_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_BenutzerRechteprofile_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BenutzerRechteprofile_Rechtevorlagenversionen_VorlageId",
                        column: x => x.VorlageId,
                        principalTable: "Rechtevorlagenversionen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Rechtevorlagenversionen",
                columns: new[] { "Id", "Bereich", "Name", "RechteJson", "Version" },
                values: new object[,]
                {
                    { "buero:1", "Kontakte", "Büromitarbeiter", "{\"Lesen\":true,\"Erstellen\":true,\"Bearbeiten\":true,\"Archivieren\":false}", 1 },
                    { "teamleitung:1", "Kontakte", "Teamleitung", "{\"Lesen\":true,\"Erstellen\":true,\"Bearbeiten\":true,\"Archivieren\":true}", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenutzerRechteprofile_VorlageId",
                table: "BenutzerRechteprofile",
                column: "VorlageId");

            migrationBuilder.CreateIndex(
                name: "IX_Berechtigungsaenderungen_AkteurId",
                table: "Berechtigungsaenderungen",
                column: "AkteurId");

            migrationBuilder.CreateIndex(
                name: "IX_Berechtigungsaenderungen_ZielId",
                table: "Berechtigungsaenderungen",
                column: "ZielId");

            // Snapshot only pre-existing employees. New accounts have no profile and fail closed.
            // Existing resource filters and IArbeitsrechte still apply; no data grants are inserted.
            migrationBuilder.Sql("""
                INSERT INTO "BenutzerRechteprofile" ("BenutzerId","Bereich","VorlageId","Modus","AbweichungenJson","Version")
                SELECT "Id", 'Kontakte', NULL, 'Bestand', '{"Lesen":true,"Erstellen":true,"Bearbeiten":true,"Archivieren":true}', 1
                FROM "Benutzer" WHERE "Rolle" = 'Mitarbeiter';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
                migrationBuilder.Sql("""
                    DO $$ BEGIN
                      IF EXISTS (SELECT 1 FROM "BenutzerRechteprofile" WHERE "Modus" <> 'Bestand')
                         OR EXISTS (SELECT 1 FROM "Berechtigungsaenderungen") THEN
                        RAISE EXCEPTION 'Explicit permissions exist; reviewed restore or forward migration required.';
                      END IF;
                    END $$;
                    """);
            migrationBuilder.DropTable(
                name: "BenutzerRechteprofile");

            migrationBuilder.DropTable(
                name: "Berechtigungsaenderungen");

            migrationBuilder.DropTable(
                name: "Rechtevorlagenversionen");
        }
    }
}
