# The Switchboard — Vertical Slice Plan (Design-Locked)

**Design:** locked to `wireframes/design-32e-newsprint.html` (single-page marketing + Privacy/Terms/Accessibility).
**Principle:** Every slice ships with admin UI + public surface + passing tests + a demonstrable delta. User confirms each slice before the next begins. **TDD RGR ruthlessly** — failing test first, then minimal code to green, then refactor.
**Stack:** .NET 9 · ASP.NET Core 9 · Razor Pages (SSR) · PostgreSQL 17 / EF Core 9 · Tailwind CSS 4 · Alpine.js · Railway · MailKit + Amazon SES · Phoenix CRM webhook · Serilog + Seq.
**Forbidden:** "lead / leads" in any customer-facing copy · third-party SaaS dependencies at runtime · skipping tests · batching multiple slices without user sign-off.

---

## Slice 1 — Foundation + Static Render *(~2 days)*
**Goal:** the wireframe renders from Razor Pages behind a real app with DB, admin login, security headers, health check, site settings CRUD, Serilog, and dev admin seed. Zero functional change to the public eye — just moves the wireframe off static HTML onto the real stack.

### Ships
- ASP.NET Core 9 scaffold · Dockerfile · `railway.toml` · `docker-compose.yml`
- EF Core 9 + Npgsql · initial migration · seed
- Security headers middleware (CSP, X-Frame-Options, X-XSS, Referrer-Policy)
- Health check at `/health`
- Razor Pages for `/` (design-32e wired in) · `/privacy` · `/terms` · `/accessibility`
- Static assets served from `wwwroot/`
- **Admin: login/logout** (cookie auth, Argon2id hash, 5-attempt lockout)
- **Admin: dashboard** (page views, submission count, content counts)
- **Admin: site settings CRUD** (phone, email, address, social URLs, hero copy, deck copy, editorial copy, pull quote)
- Serilog + Seq
- Dev admin seeded from `appsettings.Development.json`

### BDD scenarios (18)
```gherkin
S1-01 CSP + security headers set on all responses
S1-02 /health returns 200
S1-03 / renders with title + meta description; visitor scorecard pill initializes
S1-04 Header nav links route to the correct in-page anchors
S1-05 /privacy returns 200 and renders legal content from DB
S1-06 /terms returns 200 and renders legal content from DB
S1-07 /accessibility returns 200 and renders legal content from DB
S1-08 Unknown route → 404 page
S1-09 Unhandled exception → 500 page, logged to Seq with request ID
S1-10 /admin/login renders
S1-11 Valid admin creds → redirect to /admin/dashboard
S1-12 Invalid admin creds → error message, stays on login
S1-13 5 failed attempts → lockout 15 minutes
S1-14 Unauthenticated /admin/* → 302 to /admin/login
S1-15 Admin dashboard shows metric cards
S1-16 Admin edits site settings → saved to DB
S1-17 Footer renders phone + address from SiteSettings after admin edit
S1-18 No "lead/leads" in any public-facing text (grep guard in CI)
```

### You can test
Log in → edit address in Site Settings → visit `/` → new address in footer.

---

## Slice 2 — Contact Form → DB → Email → Phoenix CRM *(~3 days · highest-value)*
**Goal:** the form actually works end-to-end. Every submission lands in DB, fires the Phoenix CRM webhook, sends a confirmation email to the submitter and a notification to the team. The client-side vignette animation only triggers after a real server 200.

### Ships
- `ContactSubmission` entity (name, role, phone, email, message, IP, UA, source page, timestamps, Phoenix sync status)
- `POST /api/contact` — FluentValidation + `HtmlSanitizer` + rate limit (10/min/IP)
- Honeypot field + submit-timing check (no CAPTCHA)
- MailKit → Amazon SES: confirmation email to submitter + internal notification
- Phoenix CRM webhook dispatch with retry queue + dead-letter table
- Form frontend rewired: vignette shows only after server 200, error path shows an inline error state
- **Admin: submissions list** — paginated, filter by role, search name/email
- **Admin: submission detail** — full payload, Phoenix sync status, raw request headers
- **Admin: "Phoenix test"** button — pings the CRM with a dummy payload
- **Admin: email template editor** — confirmation + notification templates

### BDD scenarios (22)
```gherkin
S2-01 POST /api/contact w/ valid payload → 200, row saved, IP + UA captured
S2-02 Invalid email → 400 w/ field error
S2-03 Missing required field → 400
S2-04 XSS in message → sanitized on write + render
S2-05 SQL-injection attempt → parameterized query handles it, no issue
S2-06 11th request from same IP inside 60s → 429
S2-07 Honeypot filled → silently dropped, 200 response (don't tip off bots)
S2-08 Submit-timing <2s → treated as bot, dropped
S2-09 Valid submit → vignette renders only after server 200
S2-10 Server 500 → inline error, form stays editable, vignette does NOT play
S2-11 TCPA consent paragraph present on form
S2-12 Phone optional; if provided, must validate as E.164
S2-13 Phoenix webhook fires with correct payload shape
S2-14 Phoenix webhook 500 → queued for retry, submission still saved
S2-15 Retry queue: 3 attempts w/ backoff → dead-letter on final failure
S2-16 Confirmation email sent to submitter via SES
S2-17 Notification email sent to team via SES
S2-18 Admin submissions list paginated (25/page), sortable by date
S2-19 Admin filter by role (carrier/agency/MGA) works
S2-20 Admin detail view shows full payload + Phoenix sync state
S2-21 Admin Phoenix test button pings CRM, displays response
S2-22 SES bounce webhook → submission flagged in admin
```

### You can test
Fill the form → see vignette only after real network call → check admin submissions list → verify email in inbox → verify Phoenix received webhook → click Phoenix test button in admin.

---

## Slice 3 — CMS: Homepage + Legal Pages *(~3 days)*
**Goal:** founders can edit every piece of public copy from admin. Design stays locked; copy becomes DB-driven.

### Ships
- DB-back every piece of homepage copy: hero headline/deck · editorial two-column body · pull quote · intel-rail 5 step titles+bodies · audience-strip 3 bodies · CTA deck · footer copy
- **Admin: homepage content editor** (field-by-field, markdown per field)
- **Admin: roster/ecosystem CRUD** (name, logo upload via `ImageSharp`, sort order, active toggle)
- **Admin: Phoenix KPI config** (static baseline: 18,447 / 97.1% / 17,921 / 3.8ms / 9.2ms / 99.99%)
- **Admin: legal pages editor** (TipTap rich text for Privacy / Terms / Accessibility)
- **Admin: image upload pipeline** (ImageSharp → auto WebP + thumb/med/large)
- Version history per content field (last 10 edits, one-click revert)
- Output cache w/ admin-save invalidation

### BDD scenarios (20)
```gherkin
S3-01 Every piece of homepage text renders from DB, not hardcoded
S3-02 Admin → edit hero headline → public page updates after cache bust
S3-03 Roster CRUD: create, upload logo, edit, delete
S3-04 Active roster logos appear in ecosystem carousel in sort order
S3-05 Active toggle hides logo from public
S3-06 Image upload auto-generates WebP + 3 sizes
S3-07 Image upload rejects non-image or > 5MB
S3-08 Phoenix KPI config edit reflected in terminal
S3-09 Privacy page from DB, rich text rendered server-side
S3-10 Terms page from DB
S3-11 Accessibility page from DB
S3-12 Version history shows last 10 edits per field
S3-13 Revert restores prior version
S3-14 Public pages cached 5m, bust on admin save
S3-15 Admin edit emits SignalR "preview" event (optional)
S3-16 Empty required field → validation error
S3-17 XSS in rich text sanitized by HtmlSanitizer
S3-18 Concurrent edit → last-write-wins, conflict warning shown
S3-19 Unauthenticated /admin/content → 302 to login
S3-20 Save blocked if customer-facing text contains "lead/leads"
```

### You can test
Admin → Homepage → change "Insurance is not a data problem." to anything else → save → visit `/` → new headline. Same for legal pages. Upload a new partner logo → see it in carousel.

---

## Slice 4 — Analytics + Observability + SEO *(~2 days)*
**Goal:** we know what's happening in production. Search engines + LLMs know what the site is.

### Ships
- Anonymous session analytics (hashed IP, UA-bucket, visitor fingerprint hash, scroll depth)
- **Admin: analytics dashboard** (sessions, top pages, scroll-depth histogram, form conversion rate, daily/weekly chart)
- **Admin: form funnel** (impression → scroll → submit)
- `sitemap.xml` — generated from Razor Pages route table
- `robots.txt` (allow root, disallow `/admin`)
- `llms.txt` — human-readable site summary for AI crawlers
- JSON-LD schema: Organization, Product, FAQPage
- Per-page meta tags + OG image generation
- Serilog enrichers (request ID, visitor fingerprint, admin user ID)
- Seq dashboard + alerting rules
- Error tracking (roll our own with Serilog + Seq)

### BDD scenarios (16)
```gherkin
S4-01 /sitemap.xml returns all public routes
S4-02 /robots.txt allows indexing, disallows /admin
S4-03 /llms.txt describes site + products
S4-04 Every page has Organization JSON-LD
S4-05 Meta description unique per page
S4-06 OG image renders for each page
S4-07 Analytics session created on first page view
S4-08 Respects Do Not Track header
S4-09 Admin analytics dashboard renders today/week/month
S4-10 Top pages sorted by views
S4-11 Form funnel: impression → scroll → submit counts
S4-12 Scroll depth bucketed (25/50/75/100)
S4-13 Errors log to Seq with full context
S4-14 Critical errors → email alert
S4-15 Core Web Vitals recorded per route
S4-16 Unique visitor count (hash-based, no cookies)
```

### You can test
Visit the site → check admin analytics → see your session logged → force an error → see it in Seq with full context → view `sitemap.xml`.

---

## Slice 5 — A/B + Polish + Launch *(~3 days)*
**Goal:** production-ready. DNS cutover to `theswitchboardmarketing.com`.

### Ships
- A/B testing middleware (cookie-based, sticky, server-rendered variants)
- **Admin: A/B experiments CRUD** (variants, traffic split, conversion goal)
- **Admin: A/B results** (conversion rate per variant, significance calc)
- Core Web Vitals optimization pass (LCP < 2.5s, INP < 200ms, CLS < 0.1)
- WCAG 2.1 AA audit + fixes (axe-core CI check)
- Cross-browser QA (Chrome, Firefox, Safari, Edge, iOS Safari, Android Chrome)
- Preload critical assets, defer non-critical JS
- Old-site 301 redirect map
- Namecheap DNS cutover checklist
- Production smoke tests
- **Admin: feature flags** (toggle scorecard, desk cards, etc. without deploy)

### BDD scenarios (14)
```gherkin
S5-01 A/B cookie set on first visit, sticky for session
S5-02 Variant A/B served ~50/50 over 1000 visits
S5-03 Admin creates experiment with 2+ variants
S5-04 Admin sees conversion rate + significance
S5-05 Lighthouse Performance ≥ 90 on homepage
S5-06 Lighthouse Accessibility ≥ 95
S5-07 axe-core CI check passes (0 critical violations)
S5-08 LCP < 2.5s on 3G-throttled homepage
S5-09 INP < 200ms on all interactive elements
S5-10 CLS < 0.1 on all pages
S5-11 Old URLs → 301 to new equivalents
S5-12 Production Railway deploy succeeds
S5-13 Production /health returns 200
S5-14 Zero instances of "lead" in final content audit
```

### You can test
Create an A/B experiment on the hero headline → two browsers render different variants → admin shows conversion data → Lighthouse ≥ 90 → DNS cut → site live on the production domain.

---

## Summary

| Slice | Ships                                              | BDD | Effort     |
|-------|----------------------------------------------------|-----|------------|
| 1     | Foundation · login · site settings                 | 18  | ~2 days    |
| 2     | Contact form → real backend + Phoenix CRM           | 22  | ~3 days    |
| 3     | CMS: homepage + roster + legal pages                | 20  | ~3 days    |
| 4     | Analytics + SEO + observability                     | 16  | ~2 days    |
| 5     | A/B + polish + launch                               | 14  | ~3 days    |
| **Total** | **5 slices**                                   | **90** | **~13 working days** |

## Critical path
```
Slice 1 → Slice 2 → Slice 3 → Slice 4 → Slice 5
```
Serial — each slice depends on the prior's admin + DB foundation.

## Dependencies outside code
- Amazon SES account + verified sender domain
- Phoenix CRM webhook URL + auth token
- Railway PostgreSQL (already provisioned)
- Namecheap DNS access for final cutover
- Seq instance (self-host on Railway or managed)
