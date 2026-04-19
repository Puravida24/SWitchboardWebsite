# The Switchboard Website

Insurance intelligence platform — marketing site + self-hosted first-party
analytics, TCPA-grade consent certificate system, admin reporting surface,
and session replay. Built to be owned end-to-end: no third-party SaaS in the
critical path.

Live at [theswitchboardmarketing.com](https://www.theswitchboardmarketing.com/).

---

## Stack

| Layer        | Choice                                         |
|--------------|------------------------------------------------|
| Runtime      | .NET 9 / ASP.NET Core Razor Pages (SSR)        |
| ORM          | EF Core 9 (Npgsql)                             |
| Database     | PostgreSQL 17 on Railway · InMemory for tests  |
| CSS          | Tailwind CSS 4 (CLI build)                     |
| Client JS    | Vanilla + Alpine.js for micro-interactivity    |
| Session replay | Self-hosted rrweb (20% sample, masked)       |
| Real-time dashboard | SignalR                                 |
| Logging      | Serilog → Seq                                  |
| Email        | MailKit + Amazon SES                           |
| CRM          | Phoenix CRM (webhook)                          |
| Hosting      | Railway (auto-deploy on push to `main`)        |
| DNS          | Squarespace Domains                            |

---

## Quick start

```bash
# Clone + restore
git clone https://github.com/Puravida24/SWitchboardWebsite.git
cd SWitchboardWebsite
dotnet tool restore

# Tailwind (one-shot or --watch)
npm install
npm run css:build

# Run — defaults to InMemory DB when no DATABASE_URL is set
DOTNET_ROLL_FORWARD=LatestMajor dotnet run --project src/TheSwitchboard.Web
# → http://localhost:5000
```

The app seeds an admin user on first boot from `Admin:Email` / `Admin:Password`
config (env or `appsettings.Development.json`). **It will refuse to boot if no
admin exists AND `Admin:Password` is unset** — intentional, prevents a default
credential shipping to prod. See [`CLAUDE.md`](CLAUDE.md) for project rules.

---

## Tests

```bash
# Full suite
DOTNET_ROLL_FORWARD=LatestMajor dotnet test

# Only the fast non-browser tests (skips the A1 Playwright harness)
dotnet test --filter "Category!=Playwright"

# Only the real-browser tests
dotnet test --filter "Category=Playwright"
```

Current baseline: **~290 passing, 10 skipped**. The 10 skipped are external-
dependency placeholders (Lighthouse budgets → `A6` CI; webhook deliverability
→ runbook; etc.) — every Skip message points at its real owner.

### Mutation testing (on demand)

```bash
# Scan one file (~30 s)
dotnet stryker --mutate '**/PiiRedactor.cs'

# Scan everything (~30 min)
dotnet stryker
```

See [`stryker-config.json`](stryker-config.json) for excludes + thresholds.

---

## Deploy

Push to `main` → Railway auto-deploys. Three GitHub Actions workflows run in
parallel on every push:

| Workflow                     | What it does                                  |
|------------------------------|-----------------------------------------------|
| `deploy-annotate.yml`        | Posts a DeployChange row to prod for the admin ChangesLog timeline |
| `lighthouse.yml` (A6)        | Lighthouse CI audit against live URL (perf / a11y / SEO / best-practices) |
| `zap-baseline.yml` (A12)     | OWASP ZAP baseline security scan against live URL |

Both Lighthouse and ZAP wait 150s after push for Railway to finish the deploy
before auditing.

---

## Project layout

```
src/
  TheSwitchboard.Web/              — The application
    Api/                           — Minimal API endpoints (contact, consent, ops, tracking, indexnow)
    BackgroundServices/            — Rollup + Retention + Insights hosted services
    Data/                          — AppDbContext, AdminSeedService
    Middleware/                    — Security headers, CSP nonce, rate limit, analytics, A/B testing
    Models/                        — Entities (Forms, Site, Tracking, Pages)
    Pages/                         — Razor Pages (public + Admin/*)
    Services/                      — Analytics services (one per admin report page)
    Hubs/                          — SignalR RealtimeHub
    wwwroot/
      js/tracker/                  — First-party tracker modules (identity, pageview, clickstream, forms, ...)
      js/vendor/                   — Self-hosted rrweb, web-vitals, chart.js, signalr
      wireframes/                  — Static homepage HTML + assets (design-32e-newsprint.html is the live home)
  TheSwitchboard.Web.Tests/        — xUnit + Microsoft.Playwright

.github/workflows/                 — CI (deploy-annotate, lighthouse, zap-baseline)
.config/dotnet-tools.json          — Pinned local tools (Stryker)
stryker-config.json                — Mutation-testing config
.lighthouserc.json                 — Lighthouse CI budgets
.zap/rules.tsv                     — OWASP ZAP rule overrides
```

---

## Further reading

Every doc in this repo has a single, specific purpose — skim the index:

| Doc                         | Purpose                                        |
|-----------------------------|------------------------------------------------|
| [`CLAUDE.md`](CLAUDE.md)    | **Workflow rules** (TDD RGR, vertical slicing, Memento, brand rules) |
| [`docs/architecture.md`](docs/architecture.md) | Middleware chain, tracking platform, slice model, security posture |
| [`RUNBOOK.md`](RUNBOOK.md)  | Operational procedures (Phoenix match, retention, alerts, DSR, Seq) |
| [`LAUNCH_CHECKLIST.md`](LAUNCH_CHECKLIST.md) | Pre/post-launch manual verifications |
| [`VERTICAL_SLICE_PLAN.md`](VERTICAL_SLICE_PLAN.md) | T-1..T-12 + T-7B/C + H-* + A-* slice history |
| [`WEBSITE_AUDIT_REPORT_CARD.md`](WEBSITE_AUDIT_REPORT_CARD.md) | Graded rubric across code / tests / perf / security / docs |
| [`PROJECT_SCOPE.md`](PROJECT_SCOPE.md) | Original scope + audit findings |

---

## License

Proprietary — The Switchboard, LLC (Orem, UT). All rights reserved.
