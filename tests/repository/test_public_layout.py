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
            text = (ROOT / name).read_text(encoding='utf-8')
            for pattern in patterns:
                self.assertIsNone(re.search(pattern, text),
                                  'Public file contains a private-path or secret indicator')


if __name__ == '__main__':
    unittest.main()
