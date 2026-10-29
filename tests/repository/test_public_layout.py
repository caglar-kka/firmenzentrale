"""Offline checks for the public repository boundary; no private files required."""
from pathlib import Path
import os
import re
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
PUBLIC_FILES = {
    'src/backend/Betriebsplattform.Web/Sicherheit/Kontaktberechtigungen.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Berechtigungsmodell.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261029153257_Kontaktberechtigungen.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261029153257_Kontaktberechtigungen.Designer.cs',
    'src/frontend/src/Kontaktrechte.tsx',
    'src/frontend/tests/kontaktrechte.test.mjs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/KontaktberechtigungenTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/KontaktrechtePostgreSqlTests.cs',
    'docs/public/adr/0007-kontaktberechtigungen.md',

    "tests/backend/Betriebsplattform.Rechnungen.Tests/PraxisabnahmeTests.cs",
    "src/backend/Betriebsplattform.Web/Sicherheit/LokaleAdminAbnahme.cs",
    "tests/backend/Betriebsplattform.Rechnungen.Tests/LokaleAdminAbnahmeTests.cs",
    "src/backend/Betriebsplattform.Web/Betrieb/LokalerAdminErstzugang.cs",
    "tests/backend/Betriebsplattform.Rechnungen.Tests/LokalerAdminTests.cs",
    'src/frontend/tests/erinnerungen.test.mjs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023122655_TerminadresseUndErinnerung.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023122655_TerminadresseUndErinnerung.Designer.cs',
    'src/frontend/src/Terminerinnerungen.tsx',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023121507_PasswortMfaBindung.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023121507_PasswortMfaBindung.Designer.cs',
    'src/backend/Betriebsplattform.Web/Betrieb/IsolierteArbeitsumgebung.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023035136_Objektpflege.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023035136_Objektpflege.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023035758_Leistungskatalog.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023035758_Leistungskatalog.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023013041_NachweisFreigabeIdGrenze.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023013041_NachweisFreigabeIdGrenze.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023013250_PrivateNachweisablage.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023013250_PrivateNachweisablage.Designer.cs',

    'src/backend/Betriebsplattform.Web/Leistungen/Leistungsmodell.cs',
    'src/backend/Betriebsplattform.Web/Leistungen/Leistungsdienst.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Leistungsmodell.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023011313_Leistungsnachweise.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261023011313_Leistungsnachweise.Designer.cs',
    'src/frontend/src/Leistungsnachweise.tsx',
    'src/frontend/tests/leistungsnachweise.test.mjs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/LeistungsnachweiseTests.cs',

    'src/backend/Betriebsplattform.Web/Sicherheit/Bildverwaltung.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/BildverwaltungTests.cs',
    'src/frontend/src/Bildverwaltung.tsx',
    'src/frontend/tests/bildverwaltung.test.mjs',

    'src/backend/Betriebsplattform.Web/Aufgaben/Termine.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Terminmodell.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261022143440_EigenstaendigeTermine.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261022143440_EigenstaendigeTermine.Designer.cs',
    'src/frontend/src/Termine.tsx',
    'src/frontend/src/Terminzeit.ts',
    'src/frontend/tests/termine.test.mjs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/TermineTests.cs',

    'src/backend/Betriebsplattform.Web/Organisation.cs',
    'src/backend/Betriebsplattform.Web/Aufgaben/Kalender.cs',
    'src/frontend/src/Module.tsx',
    'src/frontend/src/Kalender.tsx',
    'docs/public/adr/0006-begrenzte-lokale-assistenz.md',
    'src/frontend/public/mbop-kommandobruecke.png',
    'src/frontend/src/Arbeitsuebersichten.tsx',
    'src/frontend/src/Assistent.tsx',
    'src/frontend/tests/assistenz.test.mjs',
    'src/backend/Betriebsplattform.Web/Assistenz/LokaleInferenz.cs',
    'src/backend/Betriebsplattform.Web/Assistenz/Assistenzkontext.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/AssistenzTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/AssistenzHttpTests.cs',

    'src/frontend/src/Brand.tsx',
    'src/frontend/src/Dashboard.tsx',
    'src/frontend/tests/dashboard.test.mjs',
    'docs/public/adr/0005-lokale-dokumentanalyse-und-kontrollierte-muster.md',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261021030000_PersistenterScanordner.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261021030000_PersistenterScanordner.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261021020000_Dokumentmuster.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261021020000_Dokumentmuster.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/WindowsScannerTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/PersistenterScanordnerTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/DokumentpaketTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/DokumentmusterTests.cs',
    'src/frontend/tests/dokumentverwaltung.test.mjs',
    'src/frontend/src/Scanquellen.tsx',
    'src/frontend/src/Dokumentmuster.tsx',
    'src/backend/Betriebsplattform.Web/Persistenz/Scanauftragsmodell.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Dokumentmustermodell.cs',
    'src/backend/Betriebsplattform.Web/Akten/WindowsScanner.cs',
    'src/backend/Betriebsplattform.Web/Akten/PersistenterScanordner.cs',
    'src/backend/Betriebsplattform.Web/Akten/Dokumentmuster.cs',
    'src/backend/Betriebsplattform.Web/Akten/Dokumentanalyse.cs',
    'src/backend/Betriebsplattform.Web/Akten/LokaleOcr.cs',
    'src/backend/Betriebsplattform.Web/Akten/LokalerPdfAdapter.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261021010000_Dokumentanalyse.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261021010000_Dokumentanalyse.cs',
    'src/frontend/src/Dokumentenpruefung.tsx',
    'src/frontend/tests/dokumentpruefung.test.mjs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/DokumentOcrTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/DokumentanalyseTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/DokumentpruefungPersistenzTests.cs',

    'tests/backend/Betriebsplattform.Rechnungen.Tests/AuthentifizierterBrowserTests.cs',
    'src/backend/Betriebsplattform.Web/Akten/Eingangsworkflow.cs',
    'src/backend/Betriebsplattform.Web/Akten/SynthetischeEingaenge.cs',
    'src/backend/Betriebsplattform.Web/Aufgaben/AufgabenDienst.cs',
    'src/backend/Betriebsplattform.Web/Aufgaben/AufgabenEndpunkte.cs',
    'src/backend/Betriebsplattform.Web/Aufgaben/Aufgabenmodell.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Aufgabenmodell.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Eingangsmodell.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019170000_MitarbeiterErstzugang.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019170000_MitarbeiterErstzugang.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019190000_PasswortWiederherstellung.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019190000_PasswortWiederherstellung.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019200000_AuthenticatorMfa.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019200000_AuthenticatorMfa.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019210000_MfaRichtlinieUndAufgaben.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019210000_MfaRichtlinieUndAufgaben.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019223340_PosteingangWorkflow.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019223340_PosteingangWorkflow.cs',
    'src/backend/Betriebsplattform.Web/Properties/launchSettings.template.json',
    'src/backend/Betriebsplattform.Web/Sicherheit/Erstzugang.cs',
    'src/backend/Betriebsplattform.Web/Sicherheit/MfaDienst.cs',
    'src/backend/Betriebsplattform.Web/Sicherheit/MfaEndpunkte.cs',
    'src/backend/Betriebsplattform.Web/Sicherheit/MfaNotfall.cs',
    'src/backend/Betriebsplattform.Web/Sicherheit/MfaRichtlinie.cs',
    'src/backend/Betriebsplattform.Web/Sicherheit/Totp.cs',
    'src/backend/Betriebsplattform.Web/Startmodus.cs',
    'src/frontend/src/Aufgaben.tsx',
    'src/frontend/src/AuthenticatorQr.tsx',
    'src/frontend/src/Eingangsbezug.tsx',
    'src/frontend/src/Erstzugang.tsx',
    'src/frontend/src/MfaAnmeldung.tsx',
    'src/frontend/src/MfaNotfall.tsx',
    'src/frontend/src/MfaRichtlinie.tsx',
    'src/frontend/src/MfaSicherheit.tsx',
    'src/frontend/src/Posteingang.tsx',
    'src/frontend/src/PosteingangVorschau.ts',
    'src/frontend/src/Vorschau.tsx',
    'src/frontend/tests/aufgaben-integration.test.mjs',
    'src/frontend/tests/aufgaben.test.mjs',
    'src/frontend/tests/authenticator-qr.test.mjs',
    'src/frontend/tests/mfa-integration.test.mjs',
    'src/frontend/tests/posteingang.test.mjs',
    'src/frontend/tests/startintegration.test.mjs',
    'src/frontend/tests/vorschau.test.mjs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/AufgabenListenTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/AufgabenNachweisTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/AufgabenTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/ErstzugangPostgreSqlTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/ErstzugangTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/FeaturepaketePostgreSqlTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/MfaHttpTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/MfaNotfallTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/MfaRichtlinieTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/MfaTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/PasswortWiederherstellungTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/PosteingangHttpTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/PosteingangPostgreSqlTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/PosteingangTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/StartintegrationTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/VorschauTests.cs',
    'src/backend/Betriebsplattform.Web/Betrieb/Betriebsverwaltung.cs',
    'src/backend/Betriebsplattform.Web/Betrieb/Dokumentbestand.cs',
    'src/backend/Betriebsplattform.Web/Betrieb/Sicherung.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019114242_PilotSecurity.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019114242_PilotSecurity.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Pilotmodell.cs',
    'src/backend/Betriebsplattform.Web/Sicherheit/Anmeldung.cs',
    'src/backend/Betriebsplattform.Web/Sicherheit/Malwarepruefung.cs',
    'src/backend/Betriebsplattform.Web/Sicherheit/Pilotzugriff.cs',
    'src/frontend/src/Anmeldung.tsx',
    'src/frontend/src/Benutzerverwaltung.tsx',
    'src/frontend/src/auth.ts',
    'src/frontend/tests/anmeldung.test.mjs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/Mvp02BSicherheitTests.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Arbeitsaktionen.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Datensaetze.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Dokumentdateien.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/DokumentDaten.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/FinanzDaten.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/FirmenAbfragen.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/FirmenDaten.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/FirmenDbContext.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019001700_InitialRelational.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/20261019001700_InitialRelational.Designer.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Migrationen/FirmenDbContextModelSnapshot.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Persistenzkonfiguration.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/Persistenzverwaltung.cs',
    'src/backend/Betriebsplattform.Web/Persistenz/PostgreSqlHost.cs',
    'src/backend/Betriebsplattform.Web/Webschutz.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/Mvp02APersistenzTests.cs',

    'src/backend/Betriebsplattform.Web/Akten/Arbeitsumgebung.cs',
    'src/backend/Betriebsplattform.Web/Akten/Beispielakten.cs',
    'src/backend/Betriebsplattform.Web/Akten/Kategorienverwaltung.cs',
    'src/backend/Betriebsplattform.Web/Akten/Kontaktverwaltung.cs',
    'src/frontend/src/Arbeitskontext.tsx',
    'src/frontend/src/Buero.tsx',
    'src/frontend/src/Dokumentauswahl.tsx',
    'src/frontend/src/Kategorien.tsx',
    'src/frontend/src/Kontakte.tsx',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/ProduktreifeTests.cs',
    'src/frontend/tests/produktreife.test.mjs',
    'docs/public/adr/0004-kontakte-kategorien-und-arbeitsumgebung.md',

    'src/backend/Betriebsplattform.Web/Akten/Aktenbestand.cs',
    'src/backend/Betriebsplattform.Web/Akten/AktenEndpoints.cs',
    'src/backend/Betriebsplattform.Web/Akten/Aktenmodell.cs',
    'src/backend/Betriebsplattform.Web/Akten/AktenSuche.cs',
    'src/backend/Betriebsplattform.Web/Akten/Dokumentpruefung.cs',
    'src/backend/Betriebsplattform.Web/Akten/Kontaktpflege.cs',
    'src/backend/Betriebsplattform.Web/Akten/Notizablage.cs',
    'src/backend/Betriebsplattform.Web/Akten/ScanOrdnerAdapter.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/AktenTests.cs',
    'src/frontend/tests/akte.test.mjs',
    'src/frontend/src/akte-api.ts',
    'src/frontend/src/Dateieingang.tsx',
    'src/frontend/src/Aktenbereich.tsx',
    'src/frontend/src/Aktenansichten.tsx',
    'src/frontend/src/Suche.tsx',
    'docs/public/adr/0003-zentrale-vorgangsakte-und-eingangsadapter.md',

    'src/backend/Betriebsplattform.Web/DemoImportbestand.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/DemoImportTests.cs',
    'docs/public/adr/0002-lokaler-demoimport-und-pdf-vorschau.md',
    'src/backend/Betriebsplattform.Web/DemoDokumente.cs', 'src/frontend/src/Dokumente.tsx',
    'src/frontend/src/Kontaktkarte.tsx', 'src/frontend/src/kontaktaktionen.ts',
    '.gitignore', '.gitattributes', 'README.md', 'README.de.md', 'CONTRIBUTING.md', 'AGENTS.md',
    'docs/public/ARCHITEKTUR.md', 'tests/repository/test_public_layout.py',
    'docs/public/adr/0001-rechnungs-und-zahlungskern.md',
    'docs/public/MVP-01-NACHWEIS.md',
    'src/backend/Betriebsplattform.slnx',
    'src/backend/Betriebsplattform.Rechnungen/Betriebsplattform.Rechnungen.csproj',
    'src/backend/Betriebsplattform.Rechnungen/Nachweis.cs',
    'src/backend/Betriebsplattform.Rechnungen/Pruefung.cs',
    'src/backend/Betriebsplattform.Rechnungen/Rechnung.cs',
    'src/backend/Betriebsplattform.Rechnungen/Rechnungskern.cs',
    'src/backend/Betriebsplattform.Rechnungen/Zahlung.cs',
    'src/backend/Betriebsplattform.Rechnungen/Zahlungsbestaetigung.cs',
    'src/backend/Betriebsplattform.Rechnungen/Zahlungszuordnung.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/Betriebsplattform.Rechnungen.Tests.csproj',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/RechnungskernTests.cs',
    'src/backend/Betriebsplattform.Import/Betriebsplattform.Import.csproj',
    'src/backend/Betriebsplattform.Import/ExcelImportvorschau.cs',
    'src/backend/Betriebsplattform.Import/Importmodell.cs',
    'src/backend/Betriebsplattform.Import/XlsxLeser.cs',
    'src/backend/Betriebsplattform.Import/Zeilenpruefung.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/SynthetischeArbeitsmappe.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/ExcelImportvorschauTests.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/ExcelSicherheitTests.cs',
    'src/backend/Betriebsplattform.Web/Betriebsplattform.Web.csproj',
    'src/backend/Betriebsplattform.Web/Program.cs',
    'src/backend/Betriebsplattform.Web/ApiAnwendung.cs',
    'src/backend/Betriebsplattform.Web/ApiModelle.cs',
    'src/backend/Betriebsplattform.Web/DemoDaten.cs',
    'src/backend/Betriebsplattform.Web/DemoArbeitsmappe.cs',
    'tests/backend/Betriebsplattform.Rechnungen.Tests/WebPrototypTests.cs',
    'src/frontend/DESIGN.md', 'src/frontend/index.html',
    'src/frontend/package.json', 'src/frontend/package-lock.json',
    'src/frontend/tsconfig.json', 'src/frontend/tsconfig.test.json',
    'src/frontend/vite.config.mjs', 'src/frontend/src/App.tsx',
    'src/frontend/src/ui.tsx', 'src/frontend/src/Rechnungen.tsx', 'src/frontend/src/Rechnungsdetails.tsx',
    'src/frontend/src/Importvorschau.tsx', 'src/frontend/src/design-tokens.css', 'src/frontend/src/Arbeitsraum.css',
    'src/frontend/src/Designprototyp.tsx', 'src/frontend/src/prototyp.css',
    'src/frontend/src/Icon.tsx', 'src/frontend/src/api.ts', 'src/frontend/src/darstellung.ts', 'src/frontend/src/main.tsx',
    'src/frontend/src/react-dom.d.ts', 'src/frontend/src/style.css',
    'src/frontend/tests/app.test.mjs',
}


class PublicLayoutTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.git = os.environ.get('BETRIEBSPLATTFORM_GIT') or shutil.which('git')
        if not cls.git:
            raise RuntimeError('Git required: set BETRIEBSPLATTFORM_GIT to an existing executable')
        cls.scratch = tempfile.TemporaryDirectory(prefix='public-layout-')
        cls.addClassCleanup(cls.scratch.cleanup)
        cls.worktree = Path(cls.scratch.name)
        subprocess.run([cls.git, 'init', '--quiet', str(cls.worktree)], check=True,
                       capture_output=True)
        shutil.copyfile(ROOT / '.gitignore', cls.worktree / '.gitignore')

    def ignored(self, paths):
        result = subprocess.run(
            [self.git, '-C', str(self.worktree), 'check-ignore', '--no-index', '-z', '--stdin'],
            input=('\0'.join(paths) + '\0').encode('utf-8'), capture_output=True,
        )
        self.assertIn(result.returncode, (0, 1), 'git check-ignore failed')
        return set(result.stdout.decode('utf-8').rstrip('\0').split('\0')) - {''}

    def test_public_files_and_future_source_are_allowed(self):
        paths = sorted(PUBLIC_FILES | {
            'src/backend/Invoice.cs', 'src/frontend/main.tsx',
            'tests/backend/InvoiceTests.cs', 'src/frontend/package.json',
        })
        self.assertEqual(set(), self.ignored(paths))

    def test_private_and_unknown_paths_are_ignored(self):
        paths = [
            'AGENTS.override.md', 'src/AGENTS.override.md', 'START_HIER.md', 'MANIFEST.sha256',
            '.agents/skills/security-review/SKILL.md', '.agents/playbook.md',
            '.project-harness/router.py', 'docs/PROJEKTKONTEXT.md',
            'docs/PRUEFBERICHT.md', 'private/backup.md', '.vs/settings.json',
            'private/ENTWICKLUNGSPLAN.md',
            'notes.md', 'new-folder/notes.md', '.github/workflows/unreviewed.yml',
            'src/internal/notes.md', 'docs/public/playbooks/private.md',
        ]
        self.assertEqual(set(paths), self.ignored(paths))

    def test_public_instructions_are_self_contained(self):
        text = (ROOT / 'AGENTS.md').read_text(encoding='utf-8')
        for term in ('React', 'TypeScript', 'Vite', '.NET 10', 'PostgreSQL 18',
                     'EF Core 10', 'Npgsql', 'Modularer Monolith', 'synthetischen',
                     'Regressionstests', 'Restore-Test', 'CONTRIBUTING.md'):
            self.assertTrue(term in text, 'Public development rule missing: ' + term)
        for private_reference in ('.agents/', 'private/',
                                  'Entwurf zur Freigabe'):
            self.assertFalse(private_reference in text, 'Private or draft reference in public instructions')

    def test_sensitive_formats_are_ignored_inside_allowed_directories(self):
        names = [
            'original.xlsx', 'original.XLSX', 'original.xlsm', 'original.xlsb',
            'original.xls', 'original.csv', 'original.CSV', 'original.tsv',
            'invoice.pdf', 'invoice.PDF', 'contract.docx', 'contract.doc',
            'document.odt', 'slides.pptx', 'backup.zip', 'backup.7z',
            'local.db', 'local.db-wal', 'local.sqlite', 'local.sqlite3-shm',
            'database.mdf', 'database.ldf', 'database.dump', 'database.sql',
            'copy.bak', '.env', '.env.local', 'local.env', '.env.example',
            'key.pem', 'key.key', 'cert.pfx', 'cert.p12', '.npmrc', '.netrc',
            'NuGet.Config', 'appsettings.json', 'appsettings.Development.json',
            'launchSettings.json', 'credentials.json', 'client-secrets.json',
            'profile.pubxml', 'profile.pubxml.user', 'session.log',
        ]
        paths = [f'{prefix}/{name}' for prefix in ('src/backend', 'tests/data', 'docs/public')
                 for name in names]
        self.assertEqual(set(paths), self.ignored(paths))

    def test_actual_public_inventory_matches_reviewed_files(self):
        paths = [p.relative_to(ROOT).as_posix() for p in ROOT.rglob('*')
                 if p.is_file() and '.git' not in p.relative_to(ROOT).parts]
        visible = set(paths) - self.ignored(paths)
        self.assertEqual(PUBLIC_FILES, visible,
                         'Public inventory changed: review new files before updating this list')

    def test_public_markdown_links_resolve_to_public_files(self):
        for name in PUBLIC_FILES:
            if not name.endswith('.md'):
                continue
            text = (ROOT / name).read_text(encoding='utf-8')
            for target in re.findall(r'\[[^\]]+\]\(([^)]+)\)', text):
                # Reviewed primary-source citations are references, never build dependencies.
                if target in {
                    # Additional primary references reviewed 2026-10-22; no downloads or build dependencies.
                    'https://docs.ollama.com/faq',
                    'https://learn.microsoft.com/en-us/windows-hardware/drivers/image/auto-configured-scanning',
                    'https://support.microsoft.com/en-gb/windows/hardware/printer/install-and-use-a-scanner-in-windows',
                    'https://twain.org/specification/',
                    'https://developer.mozilla.org/en-US/docs/Web/API/Window/print',
                    'https://ghostscript.readthedocs.io/en/latest/Devices.html',
                    'https://tesseract-ocr.github.io/tessdoc/Command-Line-Usage.html',
                    'https://www.ghostscript.com/licensing/index.html',
                    'https://learn.microsoft.com/en-us/defender-endpoint/command-line-arguments-microsoft-defender-antivirus',
                    'https://github.com/advisories/GHSA-68fv-2mgg-jv7q',
                    'https://github.com/vitejs/vite/security/advisories/GHSA-rq7h-c2jc-7f22',
                    'https://www.bundesnetzagentur.de/DE/Fachthemen/Telekommunikation/Nummerierung/DramaNumbers/artikel.html',
                    'https://learn.microsoft.com/en-us/windows-hardware/drivers/image/wia-architecture-overview',
                    'https://learn.microsoft.com/en-us/uwp/api/windows.devices.scanners',
                    'https://learn.microsoft.com/en-us/windows/apps/develop/devices-sensors/scan-from-your-app',
                }:
                    continue
                self.assertNotIn('://', target, 'External reference requires explicit source review')
                resolved = (ROOT / name).parent.joinpath(target).resolve()
                self.assertTrue(resolved.is_relative_to(ROOT))
                self.assertIn(resolved.relative_to(ROOT).as_posix(), PUBLIC_FILES)
                self.assertTrue(resolved.is_file())

    def test_no_personal_absolute_paths_or_obvious_secret_material(self):
        patterns = [r'(?i)[a-z]:[\\/]+Users[\\/]',
                    r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',
                    r'gh[pousr]_[A-Za-z0-9]{30,}', r'AKIA[0-9A-Z]{16}']
        for name in PUBLIC_FILES:
            # Scan binary PNG bytes as Latin-1 too: private-path/ASCII secret indicators still apply.
            text = ((ROOT / name).read_bytes().decode('latin-1') if name == 'src/frontend/public/mbop-kommandobruecke.png'
                    else (ROOT / name).read_text(encoding='utf-8'))
            for pattern in patterns:
                self.assertIsNone(re.search(pattern, text),
                                  'Public file contains a private-path or secret indicator')


if __name__ == '__main__':
    unittest.main()
