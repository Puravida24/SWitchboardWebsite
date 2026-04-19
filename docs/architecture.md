# Architecture

Deep reference for the TheSwitchboard.Web application. For workflow rules
(TDD RGR, vertical slicing, Memento) see [`../CLAUDE.md`](../CLAUDE.md). For
run-book procedures see [`../RUNBOOK.md`](../RUNBOOK.md).

---

## 1 — Slice-based delivery

The app was built in named, user-approved vertical slices. Each slice ships
a failing test first, minimum production code to pass it, a demonstrable
admin-UI delta, and zero carryover scope.

**Build phase (T-1 … T-12)** — tracking + reporting platform.
See [`../VERTICAL_SLICE_PLAN.md`](../VERTICAL_SLICE_PLAN.md) for the
36-page admin surface plan plus T-7B / T-7C (TCPA consent certificate) that
were inserted mid-build.

**Hardening phase (H-1 … H-9)** — audit gaps closed: admin seed fail-fast,
forwarded-headers parsing, CSRF-guarded logout, nonce-based CSP (no
`'unsafe-eval'`, no `'unsafe-inline'` on scripts), PII redaction enricher,
self-hosted fonts, CSRF Origin check, X-Robots-Tag on `/verify/*`, schema.org
markup expansion, per-page SEO titles, IndexNow push, HTML compression + OG
share image.

**A+ phase (A-1 … A-13+)** — post-launch test-and-ops maturity:
Playwright real-browser harness (A1), security verification tests (A2),
skipped-test audit (A3), test-speed cleanup (A4), Stryker.NET (A5),
Lighthouse CI (A6), security-header contract tests (A10), per-endpoint
rate-limit tuning (A11), OWASP ZAP baseline (A12), this doc (A13).

---

## 2 — Request pipeline

Middleware order in `Program.cs` is load-bearing. Top-to-bottom execution
for an inbound request to a public page:

1. **ResponseCompression** — brotli/gzip for HTML, JS, CSS, JSON.
2. **CorrelationIdMiddleware** — injects X-Correlation-Id for request tracing
   into Serilog scope.
3. **CspNonceMiddleware** — generates per-request nonce (base64), stashes on
   `HttpContext.Items[Nonce]` so SecurityHeadersMiddleware can emit it on the
   `Content-Security-Policy` header and Razor pages can bind it via
   `<script nonce="@CspNonceMiddleware.GetNonce(Context)">`.
4. **SecurityHeadersMiddleware** — writes X-Content-Type-Options, X-Frame-
   Options: SAMEORIGIN, X-XSS-Protection, Referrer-Policy, Permissions-Policy,
   Content-Security-Policy, and X-Robots-Tag: noindex, nofollow on
   `/Admin/*`, `/api/*`, `/verify/*`. Asserted route-by-route in
   `SecurityHeadersContractTests`.
5. **HtmlNoCacheMiddleware** — forces HTML responses to `Cache-Control: no-
   store, must-revalidate` so content edits never serve stale. Static assets
   are immutable-cached via `StaticFileOptions` (7 days).
6. **RedirectMiddleware** — DB-backed 301s for any renamed paths.
7. **ForwardedHeaders** (`.UseForwardedHeaders`) — Railway terminates TLS
   and forwards X-Forwarded-For / X-Forwarded-Proto. Must run before auth so
   rate-limit / analytics see the real client IP.
8. **Authentication / Authorization** — ASP.NET Core Identity.
9. **RateLimitMiddleware** — per-path + per-IP bucket. See §5.
10. **AbTestingMiddleware** — assigns sticky variants; hydrates into Razor.
11. **AnalyticsMiddleware** — awaits `IAnalyticsService.RecordPageViewAsync`
    after `_next(context)`; skips on `/api/`, `/admin/`, `/css/`, asset
    prefixes, DNT=1, Sec-GPC=1.
12. **Serilog request logging**.
13. **StaticFiles** (immutable cache policy for `/css/`, `/js/`, `/fonts/`,
    `/wireframes/assets/*`).
14. **MapRazorPages** / minimal API endpoints / SignalR hub.

---

## 3 — First-party tracker (client)

Single defer-loaded `<script nonce="..." src="/js/tracker/tracker.js">` tag.
Boots only when DNT !== '1' and Sec-GPC !== true. Module layout:

```
/wwwroot/js/tracker/
  tracker.js        Orchestrator — loads the modules below
  transport.js      Batch queue + fetch(keepalive) + sendBeacon fallback
  identity.js       sw_vid (1y) + sw_sid (30m sliding) + triple-write to
                    localStorage/sessionStorage
  attribution.js    UTM + gclid + fbclid + msclkid parse on first load,
                    persist in sessionStorage
  pageview.js       POST /api/tracking/pageview on every navigation
  signals.js        Timezone, canvas fingerprint stub, hardware specs
  clickstream.js    Every click (CSS selector, coords, viewport) — rage
                    (3 in 500ms on same selector) and dead (no mutation/
                    nav in 1s) detection
  scroll.js         25/50/75/100 milestones + max-depth-on-unload
  mousetrail.js     200ms sampled mouse pos (capped 300/session)
  forms.js          Per-field [data-tb-field] funnel — focus/blur/input/
                    paste/error/submit/abandon
  webvitals.js      Self-hosted web-vitals.iife.js → LCP/FCP/CLS/INP/TTFB
  errors.js         window.onerror + unhandledrejection + console.error
                    (stack redaction server-side via PiiRedactor)
  replay.js         Lazy-loads rrweb-record only if sampled + consented
  consent-capture.js  On form submit: disclosure snapshot, contrast,
                    coords, behavioral signals, client-side SHA-256 hash
                    of email/phone → POST /api/tracking/consent
```

Runtime budget: ~15 KB gzip baseline on public pages; ~50 KB when rrweb
lazy-loads for sampled sessions.

---

## 4 — Data model highlights

~20 tracking tables under `Models/Tracking/`. Three cohorts:

**Identity + session envelope** (never purged)
`Visitor` · `Session` · `BrowserSignal` (1:1 with Session) · `Goal` ·
`GoalConversion` · `AlertRule` · `DeployChange` · `DataSubjectRequest` ·
`EventRollupDaily` · `KnownProxyAsn` · `DisclosureVersion`

**Raw events** (90-day retention)
`PageView` (extended with 13 attribution + device cols) ·
`AnalyticsEvent` · `ClickEvent` · `ScrollSample` · `MouseTrail` ·
`FormInteraction` · `WebVitalSample`

**Replay + errors** (1-year retention; replay chunk payload soft-deleted
at 1y while the Replay envelope row survives)
`Replay` · `ReplayChunk` (BYTEA, gzipped client-side) · `JsError` ·
`AlertLog`

**TCPA consent proof** (5-year retention via `ExpiresAt`)
`ConsentCertificate` — full proof record: timestamp, element selector,
click coords, disclosure snapshot + SHA-256 text hash + WCAG contrast
ratio + viewport visibility, environment, behavioral signals,
client-side-hashed email/phone. Linked 1:1 to `FormSubmission`.

Nightly 02:00 UTC `RollupService` aggregates raw events into
`EventRollupDaily`. Nightly 03:00 UTC `DataRetentionJob` enforces the
retention table.

---

## 5 — Rate limits

`RateLimitMiddleware` applies per-IP + per-path buckets, only on `/api/*`:

| Path pattern                  | Cap/min/IP | Why                                    |
|-------------------------------|-----------:|----------------------------------------|
| `/api/tracking/*`             | 300 (per sw_sid, fallback IP) | Tracker volume is high but legit; sid-keyed so shared IPs don't throttle each other |
| `/api/consent/match`          | 60         | Phoenix dials ≥10 prospects/min in peak hours (A11)            |
| `/api/ops/deploy-change`      | 5          | Deploys are rare — tighter bucket blocks token-guessing (A11)  |
| everything else (contact, etc.) | 10       | Default                                |

Exceeding cap → 429 with `Retry-After: 60`.

Test coverage: `S2_06_RateLimit_11thRequest_Returns429`,
`RateLimitTuningTests.A11_01..04`.

---

## 6 — Auth + admin

ASP.NET Core Identity. One role (`Admin`), one seed user created at boot
from `Admin:Email` / `Admin:Password`. If no admin exists AND no password
is configured, `AdminSeedService` throws — app refuses to boot (H-01).
Intentional — prevents a default credential shipping to prod.

Admin cookie is HttpOnly + SameSite=Lax (verified by
`PlaywrightSecurityTests.A2_03_AdminLogin_Succeeds_AndIssuesLockedDownCookie`).
Login uses antiforgery token. Logout is POST-only + antiforgery (H-03).

Admin report surface lives under `/Admin/Reports/*` — 34 routes built
T-1 → T-12, one `I{Foo}AnalyticsService` per page (no query logic in
Razor). Reports are paginated + exportable via `IExportService`.

---

## 7 — Consent certificate (TCPA)

Every contact-form submission gets a `ConsentCertificate`:

```
client (consent-capture.js)
  ├── reads [data-tb-consent-disclosure] textContent + computedStyle
  ├── computes WCAG contrast ratio
  ├── checks IntersectionObserver visibility
  ├── captures click coordinates + behavioral accumulators
  ├── crypto.subtle SHA-256(email + canonicalized), same for phone
  └── POST /api/tracking/consent → receives certificateId
  
client (contact form submit)
  └── POST /api/contact with hidden certificateId field
  
server (ContactEndpoints)
  └── links ConsentCertificate ↔ FormSubmission

public
  └── GET /verify/{certificateId} renders proof (hides IP + hashes + replay)

phoenix-dial-time
  └── POST /api/consent/match (bearer-auth) → server SHA-256 hashes inputs,
      compares, returns {match, matchedFields, certificateExpired, ...}
```

Disclosure text versions are auto-detected via SHA-256 text hash — new
hash ⇒ new `DisclosureVersion` row with `Status="auto-detected"`. Admin
can then "Register" a detected version to mark it as approved.

---

## 8 — CI surface

| Workflow                     | Triggers             | Duration | Fails on                 |
|------------------------------|----------------------|----------|--------------------------|
| `deploy-annotate.yml`        | push to main         | ~2 min   | never (informational)    |
| `lighthouse.yml`             | push, PR, dispatch   | ~5 min   | a11y/SEO/best-practices below threshold |
| `zap-baseline.yml`           | push, PR, dispatch   | ~5 min   | never yet (informational baseline — will flip to fail-on-high once noise floor is known) |

Railway handles the actual deploy on push to main. CI runs are diagnostic —
they don't gate the deploy itself today.

---

## 9 — Test surface

```
src/TheSwitchboard.Web.Tests/
  Slice{1-5}IntegrationTests.cs   — BDD-style slice acceptance
  TrackerFoundationTests.cs       — T-1 identity + ping
  AttributionTests.cs             — T-2 UTM + landing flag
  SessionTests.cs                 — T-3 session upsert + bot classification
  ClickstreamTests.cs             — T-4 rage + dead click detection
  FormFunnelTests.cs              — T-5 funnel + scroll + mouse
  VitalsErrorsTests.cs            — T-6 web vitals + JS errors
  ReplayTests.cs                  — T-7 rrweb chunk ingest
  ConsentCertificateTests.cs      — T-7B
  ComplianceMatchTests.cs         — T-7C compliance + Phoenix match
  RealTimeTests.cs                — T-8 SignalR broadcast
  ReportsUiTests.cs               — T-9 admin pages render
  RollupRetentionTests.cs         — T-10
  GoalsChangesDsrTests.cs         — T-11
  InsightsAlertsTests.cs          — T-12

  SecurityHardeningTests.cs       — H-01..H-04 + H-07.*
  HardeningTests.cs               — H-1..H-8 cross-cuts
  SeoTests.cs                     — H-9 SEO surface
  SecurityHeadersContractTests.cs — A10 per-route header contract

  PlaywrightFixture.cs            — A1 real-browser subprocess harness
  PlaywrightSmokeTests.cs         — A1 homepage title
  PlaywrightSecurityTests.cs      — A2 CSP / frame-ancestors / login / cookie
  PlaywrightContactFormTests.cs   — S2-09/10 vignette + 500 handling
  RateLimitTuningTests.cs         — A11 per-endpoint bucket tuning
```

Fixture pattern: each test class owns its own `SwitchboardWebApplicationFactory`
with a unique `Database:InMemoryName` so cross-class cleanup isn't needed.

---

## 10 — Privacy posture

- **Consent banner:** none. DNT / Sec-GPC honored both client-side (tracker
  early-exits) and server-side (AnalyticsMiddleware drops).
- **Visitor ID:** pseudonymous; treated like a session ID.
- **PII:** raw email/phone is hashed client-side (`crypto.subtle`) before
  server sees it. Server never stores raw PII in tracking tables.
  `FormSubmission` stores the raw submitted values (legitimate business
  record); they're encrypted at rest via Postgres' block-level protection.
- **Session replay:** `maskAllInputs: true`, regex PII mask on text nodes,
  `[data-tb-pii]` attribute opt-in on any DOM node.
- **CIPA notice:** `/privacy` plus one-line footer on the homepage
  ("We record sessions to improve this site — see Privacy") — satisfies
  Javier v. Assurance IQ notice-before-recording precedent.
- **DSR (CCPA delete):** `/Admin/Reports/DSR` → admin enters email → service
  purges across every tracking table + logs per-table row counts.

---

## 11 — What's NOT here

The app is deliberately not a platform. Out of scope by design:

- Third-party analytics SaaS (Clarity, Mixpanel, Amplitude, Segment). Every
  signal is first-party.
- Ad-platform pixels (Meta Pixel, GA4, LinkedIn Insight, Google Ads tag).
  None served.
- Third-party CDN for fonts / JS. Everything under `/wwwroot/js/vendor/`
  and `/wwwroot/fonts/` is self-hosted + versioned.
- Consent banner / CMP. DNT and Sec-GPC headers are the consent surface.
- Phone tracking / call routing. Phoenix CRM owns that; we only verify
  consent at dial time via `/api/consent/match`.

If a future slice adds any of the above, it breaks a load-bearing decision
and requires explicit user approval first.
