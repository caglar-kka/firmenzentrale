using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betriebsplattform.Web.Persistenz.Migrationen
{
    /// <inheritdoc />
    public partial class PilotSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Benutzer",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Login = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Anzeigename = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswortHash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Rolle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Aktiv = table.Column<bool>(type: "boolean", nullable: false),
                    Sicherheitskennung = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Fehlversuche = table.Column<int>(type: "integer", nullable: false),
                    GesperrtBis = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benutzer", x => x.Id);
                    table.CheckConstraint("CK_Benutzer_Pflicht", "length(trim(\"Login\")) > 0 AND length(trim(\"Anzeigename\")) > 0 AND length(\"PasswortHash\") > 0");
                    table.CheckConstraint("CK_Benutzer_Rolle", "\"Rolle\" IN ('Administrator','Mitarbeiter')");
                });

            migrationBuilder.CreateTable(
                name: "Ressourcenfreigaben",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BenutzerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KontaktId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AkteId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DokumentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ressourcenfreigaben", x => x.Id);
                    table.CheckConstraint("CK_Freigabe_Ziel", "(CASE WHEN \"KontaktId\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"AkteId\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"DokumentId\" IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_Ressourcenfreigaben_Akten_AkteId",
                        column: x => x.AkteId,
                        principalTable: "Akten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ressourcenfreigaben_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ressourcenfreigaben_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ressourcenfreigaben_Kontakte_KontaktId",
                        column: x => x.KontaktId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sicherheitsereignisse",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BenutzerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Aktion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Ressource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Zeitpunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sicherheitsereignisse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sicherheitsereignisse_Benutzer_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "Benutzer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Benutzer_Login",
                table: "Benutzer",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ressourcenfreigaben_AkteId",
                table: "Ressourcenfreigaben",
                column: "AkteId");

            migrationBuilder.CreateIndex(
                name: "IX_Ressourcenfreigaben_BenutzerId_AkteId",
                table: "Ressourcenfreigaben",
                columns: new[] { "BenutzerId", "AkteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ressourcenfreigaben_BenutzerId_DokumentId",
                table: "Ressourcenfreigaben",
                columns: new[] { "BenutzerId", "DokumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ressourcenfreigaben_BenutzerId_KontaktId",
                table: "Ressourcenfreigaben",
                columns: new[] { "BenutzerId", "KontaktId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ressourcenfreigaben_DokumentId",
                table: "Ressourcenfreigaben",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Ressourcenfreigaben_KontaktId",
                table: "Ressourcenfreigaben",
                column: "KontaktId");

            migrationBuilder.CreateIndex(
                name: "IX_Sicherheitsereignisse_BenutzerId",
                table: "Sicherheitsereignisse",
                column: "BenutzerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sicherheitsereignisse_Zeitpunkt",
                table: "Sicherheitsereignisse",
                column: "Zeitpunkt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ressourcenfreigaben");

            migrationBuilder.DropTable(
                name: "Sicherheitsereignisse");

            migrationBuilder.DropTable(
                name: "Benutzer");
        }
    }
}
