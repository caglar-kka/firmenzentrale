using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betriebsplattform.Web.Persistenz.Migrationen
{
    /// <inheritdoc />
    public partial class PersistenterScanordner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScanauftragId",
                table: "Dokumentaktivitaeten",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Scanauftraege",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    QuelleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Dateiname = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LeaseId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Versuche = table.Column<int>(type: "integer", nullable: false),
                    LeasingBis = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DokumentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Fehlercode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scanauftraege", x => x.Id);
                    table.CheckConstraint("Scanauftraege_Version", "\"Version\" > 0");
                    table.CheckConstraint("Scanauftrag_Status", "\"Status\" IN ('Wartend','In Bearbeitung','Erledigt','Blockiert','Fehler')");
                    table.CheckConstraint("Scanauftrag_Versuche", "\"Versuche\" >= 0 AND \"Versuche\" <= 3");
                    table.ForeignKey(
                        name: "FK_Scanauftraege_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentaktivitaeten_ScanauftragId",
                table: "Dokumentaktivitaeten",
                column: "ScanauftragId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scanauftraege_DokumentId",
                table: "Scanauftraege",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Scanauftraege_QuelleId_Hash",
                table: "Scanauftraege",
                columns: new[] { "QuelleId", "Hash" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Dokumentaktivitaeten_Scanauftraege_ScanauftragId",
                table: "Dokumentaktivitaeten",
                column: "ScanauftragId",
                principalTable: "Scanauftraege",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dokumentaktivitaeten_Scanauftraege_ScanauftragId",
                table: "Dokumentaktivitaeten");

            migrationBuilder.DropTable(
                name: "Scanauftraege");

            migrationBuilder.DropIndex(
                name: "IX_Dokumentaktivitaeten_ScanauftragId",
                table: "Dokumentaktivitaeten");

            migrationBuilder.DropColumn(
                name: "ScanauftragId",
                table: "Dokumentaktivitaeten");
        }
    }
}
