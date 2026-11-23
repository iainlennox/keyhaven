# Keyhaven

**AI-assisted property management for UK letting agencies — ASP.NET Core MVC, C#, .NET 10.**

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fiainlennox%2Fkeyhaven%2Fmain%2Fdeploy%2Fazuredeploy.json)

[![CI](https://github.com/iainlennox/keyhaven/actions/workflows/ci.yml/badge.svg)](https://github.com/iainlennox/keyhaven/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-2f6655.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4)](https://learn.microsoft.com/aspnet/core/mvc/overview)

Keyhaven is a working, fully seeded property-management platform for UK letting agencies: owner onboarding, marketing, enquiries and viewings, tenancies, client money, repairs, compliance, key control and CRM — plus an inbox assistant that answers only from the records it can see.

It is a real application, not a static mock-up. Every screenshot below is rendered from the seeded demo database.

---

## Screens

![Keyhaven marketing site](docs/screenshots/01-home.jpg)
*The public site, with the live demo portfolio wired in.*

![Portfolio dashboard](docs/screenshots/03-dashboard.jpg)
*The staff workspace: rent collection, arrears, compliance, repairs and the AI inbox at a glance.*

<p align="center">
  <img src="docs/screenshots/04-properties.jpg" width="49%" alt="Property portfolio" />
  <img src="docs/screenshots/05-property-detail.jpg" width="49%" alt="Property record" />
</p>

<p align="center"><em>Every property carries its tenancy, marketing, certificates, repairs, keys and owner on one record.</em></p>

<p align="center">
  <img src="docs/screenshots/06-inbox.jpg" width="49%" alt="AI inbox" />
  <img src="docs/screenshots/07-inbox-draft.jpg" width="49%" alt="Grounded AI draft" />
</p>

<p align="center"><em>The inbox classifies each message, shows the exact supporting records and prepares a grounded draft for review.</em></p>

<p align="center">
  <img src="docs/screenshots/08-rent-arrears.jpg" width="49%" alt="Rent and arrears" />
  <img src="docs/screenshots/10-compliance.jpg" width="49%" alt="Compliance register" />
</p>

<p align="center"><em>Client money with ageing and reconciliation; a compliance register with expiries, gaps and renewal planning.</em></p>

<p align="center">
  <img src="docs/screenshots/09-maintenance.jpg" width="49%" alt="Repair record with AI triage" />
  <img src="docs/screenshots/11-viewings.jpg" width="49%" alt="Viewing diary" />
</p>

<p align="center"><em>Repairs move from tenant report to triage, quote, approval and completion; the viewing diary keeps the applicant thread.</em></p>

<p align="center">
  <img src="docs/screenshots/12-key-vault.jpg" width="49%" alt="Key vault" />
  <img src="docs/screenshots/13-tenancy.jpg" width="49%" alt="Tenancy record" />
</p>

<p align="center"><em>Keys and fobs with overdue visibility; the tenancy record with rent schedule and deposit protection.</em></p>

<p align="center">
  <img src="docs/screenshots/15-tenant-portal.jpg" width="49%" alt="Tenant portal" />
  <img src="docs/screenshots/16-contractor-portal.jpg" width="49%" alt="Contractor portal" />
</p>

<p align="center"><em>Tenants see their home, payments and repairs. Contractors see only their assigned jobs and access notes.</em></p>

<p align="center">
  <img src="docs/screenshots/14-crm.jpg" width="49%" alt="CRM timeline" />
  <img src="docs/screenshots/02-login.jpg" width="49%" alt="Demo login" />
</p>

<p align="center"><em>A CRM timeline connected to people and properties, and one-click demo access for every role.</em></p>

<p align="center"><img src="docs/screenshots/17-mobile.jpg" width="240" alt="Keyhaven on mobile" /></p>

---

## What's included

| Workflow | What the demo does |
| --- | --- |
| **Owner onboarding** | Client record, management fee, agreement signature, onboarding stages, portfolio and contact history |
| **Property marketing** | Listing copy, image, channels, publication state, availability and enquiry capture |
| **Enquiries & viewings** | Enquiry register by source, pre-screening, viewing diary, confirmations, feedback and conversion |
| **Tenant onboarding** | Referencing, Right to Rent, deposit scheme, tenancy lifecycle, notices and end-of-tenancy |
| **Client money** | Rent schedules, payment recording, reconciliation, arrears ageing, chaser drafts and statement periods |
| **Repairs** | Tenant reporting, AI triage, quotes, approval, contractor assignment, access notes and completion |
| **Certificates & compliance** | Gas Safety, EPC, EICR and HMO licence register, 30-day watchlist, portfolio score and renewals |
| **Key vault** | House keys and fobs, serials, checkout holder/reason/due date, overdue flags and check-in |
| **CRM** | Contact timeline, property-linked notes, follow-ups, contractor history and applicant feedback |

### Record-grounded AI inbox

The assistant (`Services/AiInboxService.cs`) is a transparent rules engine over the property, tenancy, repair, rent and owner records. It never calls an external model, so every decision can be inspected.

| Situation | Action |
| --- | --- |
| Enquiry for an **available** property with rent on record | **Auto-send** the routine reply |
| Contractor access question with **access instructions recorded** | **Auto-send** the recorded instructions |
| Repair update with a contractor assigned but **no appointment recorded** | **Draft**, explicitly saying no time is recorded |
| Owner statement request with a complete quarter on the ledger | **Draft** naming the quarter |
| Rent balance question | **Draft** with the exact balance and no promise of a plan |
| Viewing reschedule with a matching viewing | **Draft** with the recorded time and no promise of a new slot |
| No supporting record | **Escalate** to a person |

Every draft shows its source records, confidence score and handling mode. Auto-sent replies and manual sends are written back to the CRM timeline.

### Demo portfolio

The seed creates a realistic UK portfolio:

- 8 properties across Glasgow, Edinburgh, Bristol, Manchester, London and Leeds
- 5 active tenancies and 1 historical tenancy
- 6 months of rent charges, including partial payment and arrears
- 8 repair jobs with triage, quotes and contractor activity
- 3 missing/expired and 3 expiring compliance certificates
- 6 viewings, 5 enquiries, 16 keys and fobs, and an 8-message inbox
- 4 owners at different onboarding stages

---

## Run locally

Requires the **.NET 10 SDK**. The repo includes a `global.json` pinned to the .NET 10 feature band.

```bash
git clone https://github.com/iainlennox/keyhaven.git
cd keyhaven
dotnet run --project src/Keyhaven.Web
```

Open the URL shown in the console. The default `http` launch profile uses <http://localhost:5090>.

The first run creates `keyhaven.db` (SQLite) and seeds the demo portfolio. Reset it at any time:

```bash
dotnet run --project src/Keyhaven.Web -- --reset-demo
```

Or sign in as Admin → **Platform settings** → **Reset the demo**.

### Demo accounts

All accounts use the password **`Demo123!`**. The login page also has one-click buttons.

| Role | Email | Access |
| --- | --- | --- |
| Admin | `admin@keyhaven.co.uk` | Everything, including platform settings |
| Manager | `manager@keyhaven.co.uk` | Full portfolio workspace |
| Landlord | `landlord@keyhaven.co.uk` | Only their own properties and tenancies |
| Tenant | `tenant@keyhaven.co.uk` | Their home, payments, repairs and compliance |
| Contractor | `contractor@keyhaven.co.uk` | Only assigned jobs and access notes |

---

## Deploy to Azure

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fiainlennox%2Fkeyhaven%2Fmain%2Fdeploy%2Fazuredeploy.json)

The button opens the Azure portal and deploys [`deploy/azuredeploy.json`](deploy/azuredeploy.json), which creates:

- A **Linux App Service plan** (B1 by default, S1 optional)
- A **.NET 10 Linux web app** with HTTPS-only and a system-assigned identity
- App settings for `/home/keyhaven.db`, forwarded headers and port `8080`
- **Run from package** deployment using the latest `keyhaven-linux-x64.zip` GitHub release asset

The first request seeds a fresh demo database. This is a demo deployment: the one-click demo sign-ins are intentionally public, so do not put real client data in it. Delete the resource group when you're finished to stop charges.

To publish a new deployable version, tag a release:

```bash
git tag v1.0.1
git push origin v1.0.1
```

The **Release package** workflow builds and attaches `keyhaven-linux-x64.zip`; the Deploy to Azure button always uses the latest release asset.

---

## Architecture

```text
src/Keyhaven.Web/
├── Program.cs                     ASP.NET Core host, DI, cookie auth, database init
├── Data/
│   ├── KeyhavenDbContext.cs       EF Core model and relationships
│   └── SeedData.cs                UK portfolio, users, ledgers, certificates, inbox
├── Models/                        Entities and enums
├── Services/
│   ├── AiInboxService.cs          Classification, grounding, auto-send / draft / escalate
│   ├── ArrearsService.cs          Balances, ageing buckets and chaser drafts
│   ├── ComplianceService.cs       Required certificates, expiry states and score
│   ├── MaintenanceTriageService.cs
│   ├── DashboardService.cs        Role-aware dashboard queries
│   └── CurrentUser.cs             Claims and access-scoping helpers
├── Controllers/                   MVC controllers with server-side role scoping
├── ViewModels/                    Display and form models
├── Views/                         Razor workspace, auth, marketing and admin pages
├── TagHelpers/IconTagHelper.cs    Inline icon sprite
├── Helpers/                       Formatting, labels and status tones
└── wwwroot/                       Custom CSS, JS and editorial SVG illustrations

tests/Keyhaven.Tests/              xUnit tests for triage, compliance and arrears rules
deploy/azuredeploy.json            Deploy to Azure ARM template
```

### Stack

- **ASP.NET Core MVC** on **.NET 10**
- **EF Core 10** with **SQLite** for zero-setup demos
- **Cookie authentication** with role claims; `PasswordHasher<T>` for demo credentials
- **Server-rendered Razor** with a custom design system and minimal JS
- **No external AI dependency** — the demo assistant is deterministic and auditable

---

## Tests

```bash
dotnet test
```

The 12 focused tests cover repair triage and trade inference, compliance expiry windows and HMO requirements, and the grounded arrears chaser.

---

## Production notes

This is a demonstration build. Before using it with real client money or personal data:

- Replace SQLite with a production database and use EF Core migrations instead of `EnsureCreated`.
- Replace the demo cookie accounts with a real identity provider, MFA and a password policy.
- Move certificates, agreements and documents to encrypted object storage.
- Integrate a payment provider and Open Banking reconciliation; the demo ledger is not an accounting record.
- Add audit logging, data retention, GDPR subject-request handling and field-level permissions.
- Put the inbox assistant behind a grounded retrieval/tool layer while preserving the same policy: auto-send only routine cases with complete records, draft everything requiring judgement, escalate missing facts and log every decision.
- Add rate limiting, security headers and health checks.

All names, properties, documents and messages in the demo are fictional.

---

## Licence

[MIT](LICENSE) © 2026 Iain Lennox.

Built as a free, open-source demonstration of a modern UK lettings platform.
