[English](README.md) | [Deutsch](README.de.md)

# FirmenZentrale

A modular business operations platform for SMEs in the DACH region.

**Working product name — subject to change as the product evolves.**

## Overview

FirmenZentrale brings documents, digital case files, invoices, contacts, correspondence and business workflows into one central workspace. It targets small and medium-sized businesses in Germany, Austria and Switzerland.

The application currently focuses on German. Repository documentation is primarily English at the entry point, with this [German README](README.de.md); most technical documentation remains German. Current examples use `de-DE` formatting and EUR. This does not imply country-specific tax compliance, CHF support or international production readiness.

## Current Status

Development-stage, local-first prototype. MVP-02A adds an explicit PostgreSQL persistence foundation alongside the existing Demo mode. Only fictional/synthetic data is supported. Demo records are temporary; PostgreSQL is never silently replaced by Demo mode. Local PostgreSQL 18.6 migration, schema, CRUD, constraints, concurrency, transactional rollback, restart persistence and API/browser smoke checks have passed with synthetic data. Local offline backup/restore, framework cookie authentication and resource grants are now implemented and tested. Production pilot approval is still withheld; see the current security gates below.

## Core Capabilities

- Digital case files with documents, versioned internal notes and recorded external correspondence. Recording correspondence does not send it.
- Document inbox, direct filing to a known case, controlled uploads/drag and drop, duplicate-file checks and reassignment with history and version checks.
- Read-only previews for supported documents, loaded on selection; original invoices and payment evidence remain distinct.
- Invoices, partial payments, confirmed allocations and outstanding amounts, distinguishing incoming and outgoing payments. A manual status is not bank evidence.
- Central companies, people and contact persons, structured contact details, change proposals requiring a decision, and duplicate warnings without automatic merging.
- Tolerant global search, list filters/sorting and configurable business categories.
- A limited XLSX preview/confirmation/import/report workflow using the existing template, with explicit in-memory takeover of valid invoice rows. Payment rows remain for review.

## Tech Stack

| Area | Technology / status |
| --- | --- |
| Frontend | React, TypeScript (strict), Vite |
| Backend | ASP.NET Core, C#, .NET 10 |
| Persistence | Demo in memory; optional PostgreSQL 18 / EF Core 10.0.11 / Npgsql provider 10.0.3 |
| Structure | Modular monolith; local-first operation |

## Architecture

One React frontend and one ASP.NET Core host reuse central domain services for invoices/payments, case files, documents, contacts and import. The inbox is an intake gateway, not a permanent archive or mandatory route: documents can be filed directly when the target case is known. Internal notes and external correspondence have different meanings.

Technical namespaces and project paths still use the original internal naming. The optional database foundation is not a production or general workflow-engine approval. See the [architecture documentation](docs/public/ARCHITEKTUR.md) and [ADRs](docs/public/adr/0001-rechnungs-und-zahlungskern.md).

## Getting Started

Requirements: .NET 10 SDK, Node.js 24, npm and Git. From the repository root:

```powershell
dotnet restore src/backend/Betriebsplattform.slnx
Push-Location src/frontend
npm.cmd ci --ignore-scripts --no-audit --no-fund
npm.cmd run build
Pop-Location
```

Package restore/install requires registry access unless all required packages are cached. Add `--offline` to the npm command when using a complete local cache. This skips neither the need for dependency review nor the separately documented security checks. Testing instructions are in the [contribution guide](CONTRIBUTING.md).

## Running Locally

After the frontend build and .NET restore:

```powershell
dotnet build src/backend/Betriebsplattform.slnx --no-restore --configuration Release
dotnet run --project src/backend/Betriebsplattform.Web --configuration Release --no-build --no-restore --no-launch-profile
```

Open `http://127.0.0.1:5180`. Stop with `Ctrl+C`. The host binds to IPv4 loopback; it is not approved for public, LAN or mobile use.

For the optional synthetic scan-folder intake, append `-- --scan-demo` to the run command and place only synthetic files in `src/backend/private/scan-eingang`. The adapter reads stable files, checks duplicates and leaves source files intact. Use **Aktualisieren** in the inbox. Direct scanner control and OCR are not implemented.

For the optional PostgreSQL development mode, configuration, manual migration and live-test evidence are described in the [architecture documentation](docs/public/ARCHITEKTUR.md). No credentials belong in this repository. The default mode remains Demo.

## Development Data & Safety

Never use real customer, company, personnel, financial or credential data in the application, repository, issues or attachments. Original documents remain unchanged; uncertain contact changes, assignments and import rows require human review. No macros are executed and no payment is confirmed from Excel status text.

Document attachments currently accept PDF, JPG/JPEG, PNG, DOCX, XLSX, TXT and CSV, subject to server-side type/content/size checks (maximum 20 MB; some contents have stricter limits). Preview support depends on the format. Attaching Excel is separate from importing structured Excel data.

The structured importer supports only the synthetic, macro-free MVP-02 XLSX template: maximum 2 MiB, one visible `ISD-Testdaten` sheet and the nine columns `DatensatzId`, `Art`, `Nummer`, `Von`, `An`, `Datum`, `Betrag`, `Waehrung`, `Beleg`. It does not support arbitrary workbooks. `ISD` is a retained source/template label, not a confirmed invoice type or verified complete company name. Generated fixtures, uploads and local runtime data remain Git-excluded. No public database exposure is configured.

## Current Limitations

- Individual cookie authentication and resource authorization are implemented; production deployment, operating controls and multi-tenant isolation are not approved. The current security/pilot gate is NOT PASSED.
- Demo records, imports and histories remain temporary. PostgreSQL starts without demo seeds; the restricted XLSX importer and scan-folder adapter remain Demo-only. Local durable storage and coordinated offline backup/restore are implemented; operator access controls, independent backup media/retention and production approval remain open.
- No production email integration or sending, direct scanner integration, general OCR or AI classification.
- No complete accounting system, automated financial posting, general Excel migration or country-specific tax implementation.
- Trusted HTTPS, operating-system/backup permissions, malware integration, network restriction, independent security review and operational monitoring remain pilot gates. Pilot uploads fail closed until scanning is configured. Existing checks do not establish production readiness.

## Documentation

- [Architecture](docs/public/ARCHITEKTUR.md)
- ADRs: [invoice/payment core](docs/public/adr/0001-rechnungs-und-zahlungskern.md), [demo import and PDF preview](docs/public/adr/0002-lokaler-demoimport-und-pdf-vorschau.md), [case files and intake adapters](docs/public/adr/0003-zentrale-vorgangsakte-und-eingangsadapter.md), [contacts and categories](docs/public/adr/0004-kontakte-kategorien-und-arbeitsumgebung.md)
- [Contribution guide](CONTRIBUTING.md)
- [Development evidence and known limits](docs/public/MVP-01-NACHWEIS.md)
- [Frontend design rules](src/frontend/DESIGN.md)

## Roadmap / Next Milestone

MVP-02B2–02B4 adds real isolated backup/restore, restart inventory, individual cookie authentication, resource grants, restricted database runtime and a local publish foundation. Synthetic PostgreSQL/auth/restore checks passed; no real-data pilot is approved. The existing PostgreSQL wildcard listener must be reviewed/restricted, and trusted HTTPS, OS/backup access controls, malware scanning and final human security approval remain open. Follow the existing architecture/operations instructions; no productive data migration is authorized.

## License

No open-source license has been selected. Public availability alone does not grant an explicit open-source license.
