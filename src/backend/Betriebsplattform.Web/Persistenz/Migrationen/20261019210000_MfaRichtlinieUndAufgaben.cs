using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Betriebsplattform.Web.Persistenz.Migrationen
{
    /// <inheritdoc />
    public partial class MfaRichtlinieUndAufgaben : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MfaNeueinrichtung",
                table: "Benutzer",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Aufgaben",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Betreff = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Beschreibung = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ErstellerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ZustaendigId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Prioritaet = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FaelligAm = table.Column<DateOnly>(type: "date", nullable: true),
                    FaelligUm = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    FaelligUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Archiviert = table.Column<bool>(type: "boolean", nullable: false),
                    FirmaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RechnungId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AkteId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    KontaktId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DokumentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aufgaben", x => x.Id);
                    table.CheckConstraint("Aufgaben_Version", "\"Version\" > 0");
                    table.CheckConstraint("CK_Aufgabe_Bezug", "(CASE WHEN \"RechnungId\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"AkteId\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"KontaktId\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"DokumentId\" IS NULL THEN 0 ELSE 1 END) <= 1");
                    table.CheckConstraint("CK_Aufgabe_Prioritaet", "\"Prioritaet\" IN ('Niedrig','Normal','Hoch','Dringend')");
                    table.CheckConstraint("CK_Aufgabe_Status", "\"Status\" IN ('Offen','In Bearbeitung','Erledigt','Abgebrochen')");
                    table.CheckConstraint("CK_Aufgabe_Zeit", "\"FaelligUm\" IS NULL OR \"FaelligAm\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Aufgaben_Akten_AkteId",
                        column: x => x.AkteId,
                        principalTable: "Akten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aufgaben_Benutzer_ErstellerId",
                        column: x => x.ErstellerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aufgaben_Benutzer_ZustaendigId",
                        column: x => x.ZustaendigId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aufgaben_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aufgaben_Kontakte_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aufgaben_Kontakte_KontaktId",
                        column: x => x.KontaktId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aufgaben_Rechnungen_RechnungId",
                        column: x => x.RechnungId,
                        principalTable: "Rechnungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MfaNotfaelle",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ZielId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ZielKennung = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ZielVersion = table.Column<int>(type: "integer", nullable: false),
                    AntragstellerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GenehmigerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    GenehmigerKennung = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Grund = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Bis = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GenehmigtAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AusgefuehrtAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaNotfaelle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaNotfaelle_Benutzer_AntragstellerId",
                        column: x => x.AntragstellerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MfaNotfaelle_Benutzer_GenehmigerId",
                        column: x => x.GenehmigerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MfaNotfaelle_Benutzer_ZielId",
                        column: x => x.ZielId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MfaRegeln",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Administrator = table.Column<bool>(type: "boolean", nullable: false),
                    Mitarbeiter = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaRegeln", x => x.Id);
                    table.CheckConstraint("CK_MfaRegel_Einzel", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "Aufgabenverlauf",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AufgabeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BenutzerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Zeitpunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Vorher = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    Nachher = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aufgabenverlauf", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aufgabenverlauf_Aufgaben_AufgabeId",
                        column: x => x.AufgabeId,
                        principalTable: "Aufgaben",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aufgabenverlauf_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_AkteId",
                table: "Aufgaben",
                column: "AkteId");

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_DokumentId",
                table: "Aufgaben",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_ErstellerId",
                table: "Aufgaben",
                column: "ErstellerId");

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_FirmaId",
                table: "Aufgaben",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_KontaktId",
                table: "Aufgaben",
                column: "KontaktId");

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_RechnungId",
                table: "Aufgaben",
                column: "RechnungId");

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_ZustaendigId_Archiviert_FaelligAm",
                table: "Aufgaben",
                columns: new[] { "ZustaendigId", "Archiviert", "FaelligAm" });

            migrationBuilder.CreateIndex(
                name: "IX_Aufgabenverlauf_AufgabeId_Zeitpunkt",
                table: "Aufgabenverlauf",
                columns: new[] { "AufgabeId", "Zeitpunkt" });

            migrationBuilder.CreateIndex(
                name: "IX_Aufgabenverlauf_BenutzerId",
                table: "Aufgabenverlauf",
                column: "BenutzerId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaNotfaelle_AntragstellerId",
                table: "MfaNotfaelle",
                column: "AntragstellerId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaNotfaelle_GenehmigerId",
                table: "MfaNotfaelle",
                column: "GenehmigerId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaNotfaelle_ZielId",
                table: "MfaNotfaelle",
                column: "ZielId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Aufgabenverlauf");

            migrationBuilder.DropTable(
                name: "MfaNotfaelle");

            migrationBuilder.DropTable(
                name: "MfaRegeln");

            migrationBuilder.DropTable(
                name: "Aufgaben");

            migrationBuilder.DropColumn(
                name: "MfaNeueinrichtung",
                table: "Benutzer");
        }
    }
}
