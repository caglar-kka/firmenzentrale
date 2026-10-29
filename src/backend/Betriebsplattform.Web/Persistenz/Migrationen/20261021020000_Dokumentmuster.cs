using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betriebsplattform.Web.Persistenz.Migrationen
{
    /// <inheritdoc />
    public partial class Dokumentmuster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dokumentmuster",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OrganisationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AbsenderId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Feld = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Anker = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Strategie = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LayoutHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Dokumentart = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Quelle1Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Quelle2Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Quelle1EingangId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Quelle2EingangId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Quelle1Version = table.Column<int>(type: "integer", nullable: false),
                    Quelle2Version = table.Column<int>(type: "integer", nullable: true),
                    MusterVersion = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AutomatikAktion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    FreigabeVonId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dokumentmuster", x => x.Id);
                    table.CheckConstraint("Dokumentmuster_Version", "\"Version\" > 0");
                    table.CheckConstraint("Muster_Aktion", "\"AutomatikAktion\" IS NULL OR \"AutomatikAktion\" = 'Klassifizieren'");
                    table.CheckConstraint("Muster_Status", "\"Status\" IN ('Entwurf','Aktiv','Pausiert')");
                    table.CheckConstraint("Muster_Version", "\"MusterVersion\" > 0");
                    table.ForeignKey(
                        name: "FK_Dokumentmuster_Benutzer_FreigabeVonId",
                        column: x => x.FreigabeVonId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dokumentmuster_Dokumente_Quelle1Id",
                        column: x => x.Quelle1Id,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dokumentmuster_Dokumente_Quelle2Id",
                        column: x => x.Quelle2Id,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dokumentmuster_Eingangsworkflows_Quelle1EingangId",
                        column: x => x.Quelle1EingangId,
                        principalTable: "Eingangsworkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dokumentmuster_Eingangsworkflows_Quelle2EingangId",
                        column: x => x.Quelle2EingangId,
                        principalTable: "Eingangsworkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dokumentmuster_Kontakte_AbsenderId",
                        column: x => x.AbsenderId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dokumentmuster_Kontakte_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentmuster_AbsenderId",
                table: "Dokumentmuster",
                column: "AbsenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentmuster_FreigabeVonId",
                table: "Dokumentmuster",
                column: "FreigabeVonId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentmuster_OrganisationId_AbsenderId_Feld_MusterVersion",
                table: "Dokumentmuster",
                columns: new[] { "OrganisationId", "AbsenderId", "Feld", "MusterVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentmuster_Quelle1EingangId",
                table: "Dokumentmuster",
                column: "Quelle1EingangId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentmuster_Quelle1Id",
                table: "Dokumentmuster",
                column: "Quelle1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentmuster_Quelle2EingangId",
                table: "Dokumentmuster",
                column: "Quelle2EingangId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentmuster_Quelle2Id",
                table: "Dokumentmuster",
                column: "Quelle2Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dokumentmuster");
        }
    }
}
