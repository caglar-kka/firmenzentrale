using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betriebsplattform.Web.Persistenz.Migrationen
{
    /// <inheritdoc />
    public partial class PosteingangWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EingangsId",
                table: "Dokumentaktivitaeten",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PruefAbsenderId",
                table: "Dokumentaktivitaeten",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VorherigerAbsenderId",
                table: "Dokumentaktivitaeten",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Eingangsworkflows",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ZustaendigId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AbsenderId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AkteId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AufgabeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AngabenJson = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Rueckfrage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    GeprueftVonId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    GeprueftAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eingangsworkflows", x => x.Id);
                    table.CheckConstraint("CK_Eingang_Status", "\"Status\" IN ('Neu','Zur Prüfung','Zugeordnet','Erledigt')");
                    table.ForeignKey(
                        name: "FK_Eingangsworkflows_Akten_AkteId",
                        column: x => x.AkteId,
                        principalTable: "Akten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Eingangsworkflows_Aufgaben_AufgabeId",
                        column: x => x.AufgabeId,
                        principalTable: "Aufgaben",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Eingangsworkflows_Benutzer_GeprueftVonId",
                        column: x => x.GeprueftVonId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Eingangsworkflows_Benutzer_ZustaendigId",
                        column: x => x.ZustaendigId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Eingangsworkflows_Dokumentaktivitaeten_Id",
                        column: x => x.Id,
                        principalTable: "Dokumentaktivitaeten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Eingangsworkflows_Kontakte_AbsenderId",
                        column: x => x.AbsenderId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentaktivitaeten_EingangsId",
                table: "Dokumentaktivitaeten",
                column: "EingangsId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentaktivitaeten_PruefAbsenderId",
                table: "Dokumentaktivitaeten",
                column: "PruefAbsenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentaktivitaeten_VorherigerAbsenderId",
                table: "Dokumentaktivitaeten",
                column: "VorherigerAbsenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Eingangsworkflows_AbsenderId",
                table: "Eingangsworkflows",
                column: "AbsenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Eingangsworkflows_AkteId",
                table: "Eingangsworkflows",
                column: "AkteId");

            migrationBuilder.CreateIndex(
                name: "IX_Eingangsworkflows_AufgabeId",
                table: "Eingangsworkflows",
                column: "AufgabeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Eingangsworkflows_GeprueftVonId",
                table: "Eingangsworkflows",
                column: "GeprueftVonId");

            migrationBuilder.CreateIndex(
                name: "IX_Eingangsworkflows_ZustaendigId",
                table: "Eingangsworkflows",
                column: "ZustaendigId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dokumentaktivitaeten_Dokumentaktivitaeten_EingangsId",
                table: "Dokumentaktivitaeten",
                column: "EingangsId",
                principalTable: "Dokumentaktivitaeten",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dokumentaktivitaeten_Kontakte_PruefAbsenderId",
                table: "Dokumentaktivitaeten",
                column: "PruefAbsenderId",
                principalTable: "Kontakte",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dokumentaktivitaeten_Kontakte_VorherigerAbsenderId",
                table: "Dokumentaktivitaeten",
                column: "VorherigerAbsenderId",
                principalTable: "Kontakte",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dokumentaktivitaeten_Dokumentaktivitaeten_EingangsId",
                table: "Dokumentaktivitaeten");

            migrationBuilder.DropForeignKey(
                name: "FK_Dokumentaktivitaeten_Kontakte_PruefAbsenderId",
                table: "Dokumentaktivitaeten");

            migrationBuilder.DropForeignKey(
                name: "FK_Dokumentaktivitaeten_Kontakte_VorherigerAbsenderId",
                table: "Dokumentaktivitaeten");

            migrationBuilder.DropTable(
                name: "Eingangsworkflows");

            migrationBuilder.DropIndex(
                name: "IX_Dokumentaktivitaeten_EingangsId",
                table: "Dokumentaktivitaeten");

            migrationBuilder.DropIndex(
                name: "IX_Dokumentaktivitaeten_PruefAbsenderId",
                table: "Dokumentaktivitaeten");

            migrationBuilder.DropIndex(
                name: "IX_Dokumentaktivitaeten_VorherigerAbsenderId",
                table: "Dokumentaktivitaeten");

            migrationBuilder.DropColumn(
                name: "EingangsId",
                table: "Dokumentaktivitaeten");

            migrationBuilder.DropColumn(
                name: "PruefAbsenderId",
                table: "Dokumentaktivitaeten");

            migrationBuilder.DropColumn(
                name: "VorherigerAbsenderId",
                table: "Dokumentaktivitaeten");
        }
    }
}
