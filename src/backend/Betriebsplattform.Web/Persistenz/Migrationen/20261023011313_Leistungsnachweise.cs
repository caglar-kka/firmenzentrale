using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betriebsplattform.Web.Persistenz.Migrationen
{
    /// <inheritdoc />
    public partial class Leistungsnachweise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leistungsobjekte",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KundeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Anschrift = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BueroId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AkteId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AnlageHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leistungsobjekte", x => x.Id);
                    table.CheckConstraint("Leistungsobjekte_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Leistungsobjekte_Akten_AkteId",
                        column: x => x.AkteId,
                        principalTable: "Akten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leistungsobjekte_Benutzer_BueroId",
                        column: x => x.BueroId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leistungsobjekte_Kontakte_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Leistungsaktionen",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ObjektId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BenutzerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Art = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AnfrageHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Ergebnis = table.Column<string>(type: "character varying(200000)", maxLength: 200000, nullable: false),
                    Zeitpunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leistungsaktionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leistungsaktionen_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leistungsaktionen_Leistungsobjekte_ObjektId",
                        column: x => x.ObjektId,
                        principalTable: "Leistungsobjekte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Leistungsplaene",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ObjektId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Nummer = table.Column<int>(type: "integer", nullable: false),
                    GiltAb = table.Column<DateOnly>(type: "date", nullable: false),
                    Positionen = table.Column<string>(type: "character varying(64000)", maxLength: 64000, nullable: false),
                    AnlageHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leistungsplaene", x => x.Id);
                    table.CheckConstraint("Leistungsplaene_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Leistungsplaene_Leistungsobjekte_ObjektId",
                        column: x => x.ObjektId,
                        principalTable: "Leistungsobjekte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Leistungseinsaetze",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ObjektId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PlanId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Arbeitstag = table.Column<DateOnly>(type: "date", nullable: false),
                    MitarbeiterId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Daten = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: false),
                    AnlageHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VorgaengerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Aenderungsgrund = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leistungseinsaetze", x => x.Id);
                    table.CheckConstraint("Leistungseinsaetze_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Leistungseinsaetze_Benutzer_MitarbeiterId",
                        column: x => x.MitarbeiterId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leistungseinsaetze_Leistungseinsaetze_VorgaengerId",
                        column: x => x.VorgaengerId,
                        principalTable: "Leistungseinsaetze",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leistungseinsaetze_Leistungsobjekte_ObjektId",
                        column: x => x.ObjektId,
                        principalTable: "Leistungsobjekte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leistungseinsaetze_Leistungsplaene_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Leistungsplaene",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Leistungsnachweise",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EinsatzId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FreigegebenDurch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Zeitpunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Inhalt = table.Column<byte[]>(type: "bytea", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leistungsnachweise", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leistungsnachweise_Benutzer_FreigegebenDurch",
                        column: x => x.FreigegebenDurch,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leistungsnachweise_Leistungseinsaetze_EinsatzId",
                        column: x => x.EinsatzId,
                        principalTable: "Leistungseinsaetze",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leistungsaktionen_BenutzerId",
                table: "Leistungsaktionen",
                column: "BenutzerId");

            migrationBuilder.CreateIndex(
                name: "IX_Leistungsaktionen_ObjektId_Zeitpunkt",
                table: "Leistungsaktionen",
                columns: new[] { "ObjektId", "Zeitpunkt" });

            migrationBuilder.CreateIndex(
                name: "IX_Leistungseinsaetze_MitarbeiterId",
                table: "Leistungseinsaetze",
                column: "MitarbeiterId");

            migrationBuilder.CreateIndex(
                name: "IX_Leistungseinsaetze_ObjektId_Arbeitstag",
                table: "Leistungseinsaetze",
                columns: new[] { "ObjektId", "Arbeitstag" });

            migrationBuilder.CreateIndex(
                name: "IX_Leistungseinsaetze_PlanId",
                table: "Leistungseinsaetze",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Leistungseinsaetze_VorgaengerId",
                table: "Leistungseinsaetze",
                column: "VorgaengerId");

            migrationBuilder.CreateIndex(
                name: "IX_Leistungsnachweise_EinsatzId",
                table: "Leistungsnachweise",
                column: "EinsatzId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leistungsnachweise_FreigegebenDurch",
                table: "Leistungsnachweise",
                column: "FreigegebenDurch");

            migrationBuilder.CreateIndex(
                name: "IX_Leistungsobjekte_AkteId",
                table: "Leistungsobjekte",
                column: "AkteId");

            migrationBuilder.CreateIndex(
                name: "IX_Leistungsobjekte_BueroId",
                table: "Leistungsobjekte",
                column: "BueroId");

            migrationBuilder.CreateIndex(
                name: "IX_Leistungsobjekte_KundeId",
                table: "Leistungsobjekte",
                column: "KundeId");

            migrationBuilder.CreateIndex(
                name: "IX_Leistungsplaene_ObjektId_Nummer",
                table: "Leistungsplaene",
                columns: new[] { "ObjektId", "Nummer" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Leistungsaktionen");

            migrationBuilder.DropTable(
                name: "Leistungsnachweise");

            migrationBuilder.DropTable(
                name: "Leistungseinsaetze");

            migrationBuilder.DropTable(
                name: "Leistungsplaene");

            migrationBuilder.DropTable(
                name: "Leistungsobjekte");
        }
    }
}
