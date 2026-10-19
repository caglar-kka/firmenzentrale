using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Betriebsplattform.Web.Persistenz.Migrationen
{
    /// <inheritdoc />
    public partial class InitialRelational : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kategorien",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Suchname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Beschreibung = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ElternId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Reihenfolge = table.Column<int>(type: "integer", nullable: false),
                    Archiviert = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategorien", x => x.Id);
                    table.CheckConstraint("Kategorie_Name", "length(trim(\"Name\")) > 0");
                    table.CheckConstraint("Kategorie_Parent", "\"ElternId\" IS NULL OR \"ElternId\" <> \"Id\"");
                    table.CheckConstraint("Kategorie_Reihenfolge", "\"Reihenfolge\" >= 0 AND \"Reihenfolge\" <= 10000");
                    table.CheckConstraint("Kategorien_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Kategorien_Kategorien_ElternId",
                        column: x => x.ElternId,
                        principalTable: "Kategorien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kontakte",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Art = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Suchname = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Vorname = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Nachname = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FirmaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Strasse = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Hausnummer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Plz = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ort = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Land = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Anschrift = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Abteilung = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Rolle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Archiviert = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kontakte", x => x.Id);
                    table.CheckConstraint("Kontakt_Art", "\"Art\" IN ('Firma','Person') AND (\"FirmaId\" IS NULL OR \"Art\" = 'Person') AND (\"FirmaId\" IS NULL OR \"FirmaId\" <> \"Id\")");
                    table.CheckConstraint("Kontakt_Name", "length(trim(\"Name\")) > 0");
                    table.CheckConstraint("Kontakte_Version", "\"Version\" > 0");
                    table.CheckConstraint("Person_Name", "\"Art\" <> 'Person' OR (length(trim(\"Vorname\")) > 0 AND length(trim(\"Nachname\")) > 0)");
                    table.ForeignKey(
                        name: "FK_Kontakte_Kontakte_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Akten",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Titel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Suchtext = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    KategorieId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FirmaId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Akten", x => x.Id);
                    table.CheckConstraint("Akte_Status", "\"Status\" IN ('Offen','In Bearbeitung','Wartet auf Rückmeldung','Erledigt','Archiviert')");
                    table.CheckConstraint("Akte_Titel", "length(trim(\"Titel\")) > 0");
                    table.CheckConstraint("Akten_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Akten_Kategorien_KategorieId",
                        column: x => x.KategorieId,
                        principalTable: "Kategorien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Akten_Kontakte_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Dokumente",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Titel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Suchtext = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Dateiname = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Inhaltstyp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Groesse = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Art = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KategorieId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AbsenderId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Kanal = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Pruefbedarf = table.Column<bool>(type: "boolean", nullable: false),
                    Vorschau = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Text = table.Column<string>(type: "character varying(64000)", maxLength: 64000, nullable: false),
                    Textmethode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dokumente", x => x.Id);
                    table.CheckConstraint("Dokument_Datei", "\"Groesse\" > 0 AND \"Groesse\" <= 20000000 AND length(\"Hash\") = 64 AND length(\"StorageKey\") = 32");
                    table.CheckConstraint("Dokumente_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Dokumente_Kategorien_KategorieId",
                        column: x => x.KategorieId,
                        principalTable: "Kategorien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dokumente_Kontakte_AbsenderId",
                        column: x => x.AbsenderId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kontaktwerte",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KontaktId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Art = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Wert = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Vergleichswert = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Typ = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Primaer = table.Column<bool>(type: "boolean", nullable: false),
                    Aktiv = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kontaktwerte", x => x.Id);
                    table.CheckConstraint("Kontaktwert_Art", "\"Art\" IN ('Telefon','E-Mail','Website') AND length(trim(\"Wert\")) > 0");
                    table.ForeignKey(
                        name: "FK_Kontaktwerte_Kontakte_KontaktId",
                        column: x => x.KontaktId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Zahlungen",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ZahlerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EmpfaengerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Betrag = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Beleg = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zahlungen", x => x.Id);
                    table.CheckConstraint("Zahlung_Betrag", "\"Betrag\" > 0");
                    table.CheckConstraint("Zahlungen_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Zahlungen_Kontakte_EmpfaengerId",
                        column: x => x.EmpfaengerId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Zahlungen_Kontakte_ZahlerId",
                        column: x => x.ZahlerId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Korrespondenz",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AkteId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KontaktId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Betreff = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Inhalt = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Art = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Richtung = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Autor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KommuniziertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korrespondenz", x => x.Id);
                    table.CheckConstraint("Korrespondenz_Richtung", "\"Richtung\" IN ('Empfangen','Gesendet','Entwurf')");
                    table.CheckConstraint("Korrespondenz_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Korrespondenz_Akten_AkteId",
                        column: x => x.AkteId,
                        principalTable: "Akten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Korrespondenz_Kontakte_KontaktId",
                        column: x => x.KontaktId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notizen",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AkteId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Betreff = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Inhalt = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Autor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Archiviert = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notizen", x => x.Id);
                    table.CheckConstraint("Notiz_Text", "length(trim(\"Betreff\")) > 0 AND length(trim(\"Inhalt\")) > 0");
                    table.CheckConstraint("Notizen_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Notizen_Akten_AkteId",
                        column: x => x.AkteId,
                        principalTable: "Akten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rechnungen",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Nummer = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    AusstellerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EmpfaengerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AkteId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Richtung = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Betrag = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Zahlungsstatus = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Beleg = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rechnungen", x => x.Id);
                    table.CheckConstraint("Rechnung_Betrag", "\"Betrag\" > 0");
                    table.CheckConstraint("Rechnung_Richtung", "\"Richtung\" IN ('Eingang','Ausgang','Ungeklärt')");
                    table.CheckConstraint("Rechnung_Status", "\"Zahlungsstatus\" IN ('Offen','Teilweise bezahlt','Bezahlt')");
                    table.CheckConstraint("Rechnungen_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Rechnungen_Akten_AkteId",
                        column: x => x.AkteId,
                        principalTable: "Akten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rechnungen_Kontakte_AusstellerId",
                        column: x => x.AusstellerId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rechnungen_Kontakte_EmpfaengerId",
                        column: x => x.EmpfaengerId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Dokumentaktivitaeten",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DokumentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VorherigeAkteId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AkteId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Benutzer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Zeitpunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Kanal = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dokumentaktivitaeten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dokumentaktivitaeten_Akten_AkteId",
                        column: x => x.AkteId,
                        principalTable: "Akten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dokumentaktivitaeten_Akten_VorherigeAkteId",
                        column: x => x.VorherigeAkteId,
                        principalTable: "Akten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dokumentaktivitaeten_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Dokumentzuordnungen",
                columns: table => new
                {
                    DokumentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AkteId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dokumentzuordnungen", x => x.DokumentId);
                    table.ForeignKey(
                        name: "FK_Dokumentzuordnungen_Akten_AkteId",
                        column: x => x.AkteId,
                        principalTable: "Akten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dokumentzuordnungen_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kontaktverlauf",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KontaktId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DokumentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Vorher = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Nachher = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Aktion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Benutzer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Zeitpunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kontaktverlauf", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kontaktverlauf_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kontaktverlauf_Kontakte_KontaktId",
                        column: x => x.KontaktId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kontaktvorschlaege",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KontaktId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DokumentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Art = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Bisher = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Neu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kontaktversion = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kontaktvorschlaege", x => x.Id);
                    table.CheckConstraint("Kontaktvorschlaege_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Kontaktvorschlaege_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kontaktvorschlaege_Kontakte_KontaktId",
                        column: x => x.KontaktId,
                        principalTable: "Kontakte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Zahlungsbestaetigungen",
                columns: table => new
                {
                    ZahlungId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Beleg = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Benutzer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Zeitpunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zahlungsbestaetigungen", x => x.ZahlungId);
                    table.ForeignKey(
                        name: "FK_Zahlungsbestaetigungen_Zahlungen_ZahlungId",
                        column: x => x.ZahlungId,
                        principalTable: "Zahlungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Korrespondenzdokumente",
                columns: table => new
                {
                    KorrespondenzId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DokumentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korrespondenzdokumente", x => new { x.KorrespondenzId, x.DokumentId });
                    table.ForeignKey(
                        name: "FK_Korrespondenzdokumente_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Korrespondenzdokumente_Korrespondenz_KorrespondenzId",
                        column: x => x.KorrespondenzId,
                        principalTable: "Korrespondenz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notizrevisionen",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NotizId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Betreff = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Inhalt = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Autor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Zeitpunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Aktion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notizrevisionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notizrevisionen_Notizen_NotizId",
                        column: x => x.NotizId,
                        principalTable: "Notizen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Zahlungszuordnungen",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RechnungId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ZahlungId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Betrag = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Beleg = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Benutzer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Zeitpunkt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zahlungszuordnungen", x => x.Id);
                    table.CheckConstraint("Zahlungszuordnung_Betrag", "\"Betrag\" > 0");
                    table.ForeignKey(
                        name: "FK_Zahlungszuordnungen_Rechnungen_RechnungId",
                        column: x => x.RechnungId,
                        principalTable: "Rechnungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Zahlungszuordnungen_Zahlungsbestaetigungen_ZahlungId",
                        column: x => x.ZahlungId,
                        principalTable: "Zahlungsbestaetigungen",
                        principalColumn: "ZahlungId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Akten_FirmaId",
                table: "Akten",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_Akten_KategorieId",
                table: "Akten",
                column: "KategorieId");

            migrationBuilder.CreateIndex(
                name: "IX_Akten_Status_GeaendertAm",
                table: "Akten",
                columns: new[] { "Status", "GeaendertAm" });

            migrationBuilder.CreateIndex(
                name: "IX_Akten_Suchtext",
                table: "Akten",
                column: "Suchtext");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentaktivitaeten_AkteId",
                table: "Dokumentaktivitaeten",
                column: "AkteId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentaktivitaeten_DokumentId_Zeitpunkt",
                table: "Dokumentaktivitaeten",
                columns: new[] { "DokumentId", "Zeitpunkt" });

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentaktivitaeten_VorherigeAkteId",
                table: "Dokumentaktivitaeten",
                column: "VorherigeAkteId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumente_AbsenderId",
                table: "Dokumente",
                column: "AbsenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumente_GeaendertAm",
                table: "Dokumente",
                column: "GeaendertAm");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumente_Hash",
                table: "Dokumente",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dokumente_KategorieId",
                table: "Dokumente",
                column: "KategorieId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumente_Status_ErstelltAm",
                table: "Dokumente",
                columns: new[] { "Status", "ErstelltAm" });

            migrationBuilder.CreateIndex(
                name: "IX_Dokumente_StorageKey",
                table: "Dokumente",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dokumentzuordnungen_AkteId",
                table: "Dokumentzuordnungen",
                column: "AkteId");

            migrationBuilder.CreateIndex(
                name: "IX_Kategorien_ElternId_Reihenfolge",
                table: "Kategorien",
                columns: new[] { "ElternId", "Reihenfolge" });

            migrationBuilder.CreateIndex(
                name: "IX_Kategorien_Suchname",
                table: "Kategorien",
                column: "Suchname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kontakte_FirmaId",
                table: "Kontakte",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_Kontakte_Nachname_Vorname",
                table: "Kontakte",
                columns: new[] { "Nachname", "Vorname" });

            migrationBuilder.CreateIndex(
                name: "IX_Kontakte_Plz_Ort",
                table: "Kontakte",
                columns: new[] { "Plz", "Ort" });

            migrationBuilder.CreateIndex(
                name: "IX_Kontakte_Suchname",
                table: "Kontakte",
                column: "Suchname");

            migrationBuilder.CreateIndex(
                name: "IX_Kontaktverlauf_DokumentId",
                table: "Kontaktverlauf",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Kontaktverlauf_KontaktId_Zeitpunkt",
                table: "Kontaktverlauf",
                columns: new[] { "KontaktId", "Zeitpunkt" });

            migrationBuilder.CreateIndex(
                name: "IX_Kontaktvorschlaege_DokumentId",
                table: "Kontaktvorschlaege",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Kontaktvorschlaege_KontaktId_Status",
                table: "Kontaktvorschlaege",
                columns: new[] { "KontaktId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Kontaktwerte_Art_Vergleichswert",
                table: "Kontaktwerte",
                columns: new[] { "Art", "Vergleichswert" });

            migrationBuilder.CreateIndex(
                name: "IX_Kontaktwerte_KontaktId_Art",
                table: "Kontaktwerte",
                columns: new[] { "KontaktId", "Art" },
                unique: true,
                filter: "\"Primaer\" AND \"Aktiv\"");

            migrationBuilder.CreateIndex(
                name: "IX_Korrespondenz_AkteId_KommuniziertAm",
                table: "Korrespondenz",
                columns: new[] { "AkteId", "KommuniziertAm" });

            migrationBuilder.CreateIndex(
                name: "IX_Korrespondenz_KontaktId",
                table: "Korrespondenz",
                column: "KontaktId");

            migrationBuilder.CreateIndex(
                name: "IX_Korrespondenzdokumente_DokumentId",
                table: "Korrespondenzdokumente",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notizen_AkteId_GeaendertAm",
                table: "Notizen",
                columns: new[] { "AkteId", "GeaendertAm" });

            migrationBuilder.CreateIndex(
                name: "IX_Notizrevisionen_NotizId_Version",
                table: "Notizrevisionen",
                columns: new[] { "NotizId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rechnungen_AkteId",
                table: "Rechnungen",
                column: "AkteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rechnungen_AusstellerId_Nummer_Betrag",
                table: "Rechnungen",
                columns: new[] { "AusstellerId", "Nummer", "Betrag" });

            migrationBuilder.CreateIndex(
                name: "IX_Rechnungen_EmpfaengerId",
                table: "Rechnungen",
                column: "EmpfaengerId");

            migrationBuilder.CreateIndex(
                name: "IX_Rechnungen_Zahlungsstatus_Datum",
                table: "Rechnungen",
                columns: new[] { "Zahlungsstatus", "Datum" });

            migrationBuilder.CreateIndex(
                name: "IX_Zahlungen_Datum",
                table: "Zahlungen",
                column: "Datum");

            migrationBuilder.CreateIndex(
                name: "IX_Zahlungen_EmpfaengerId",
                table: "Zahlungen",
                column: "EmpfaengerId");

            migrationBuilder.CreateIndex(
                name: "IX_Zahlungen_ZahlerId",
                table: "Zahlungen",
                column: "ZahlerId");

            migrationBuilder.CreateIndex(
                name: "IX_Zahlungszuordnungen_RechnungId_ZahlungId",
                table: "Zahlungszuordnungen",
                columns: new[] { "RechnungId", "ZahlungId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Zahlungszuordnungen_ZahlungId",
                table: "Zahlungszuordnungen",
                column: "ZahlungId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dokumentaktivitaeten");

            migrationBuilder.DropTable(
                name: "Dokumentzuordnungen");

            migrationBuilder.DropTable(
                name: "Kontaktverlauf");

            migrationBuilder.DropTable(
                name: "Kontaktvorschlaege");

            migrationBuilder.DropTable(
                name: "Kontaktwerte");

            migrationBuilder.DropTable(
                name: "Korrespondenzdokumente");

            migrationBuilder.DropTable(
                name: "Notizrevisionen");

            migrationBuilder.DropTable(
                name: "Zahlungszuordnungen");

            migrationBuilder.DropTable(
                name: "Dokumente");

            migrationBuilder.DropTable(
                name: "Korrespondenz");

            migrationBuilder.DropTable(
                name: "Notizen");

            migrationBuilder.DropTable(
                name: "Rechnungen");

            migrationBuilder.DropTable(
                name: "Zahlungsbestaetigungen");

            migrationBuilder.DropTable(
                name: "Akten");

            migrationBuilder.DropTable(
                name: "Zahlungen");

            migrationBuilder.DropTable(
                name: "Kategorien");

            migrationBuilder.DropTable(
                name: "Kontakte");
        }
    }
}
