# The Switchboard — Vertical Slice Plan (Admin-First)

**Principle:** Every slice ships with admin CRUD + public page + testable end-to-end flow. No slice depends on a future admin build. You can test every slice the moment it lands.

**Testing each slice:** `docker-compose up` → navigate to `localhost:5000/admin` → log in → manage content → see it on the public page.

---

## Slice 1: Foundation + Site Settings Admin
**You can test:** Log into admin, edit site settings (name, tagline, contact info, social links), see values reflected in header/footer.

### Ships
- ASP.NET Core 9 scaffold, EF Core, PostgreSQL, Serilog
- Security headers middleware
- Dockerfile + docker-compose + railway.toml
- Tailwind CSS 4 with design tokens
- Layout (header, footer, mobile nav)
- Homepage shell (static hero + stats + CTA)
- 404/500 error pages
- **Admin: Login/Logout**
- **Admin: Dashboard (page views, submission count, content counts)**
- **Admin: Site Settings CRUD (site name, tagline, phone, email, address, social URLs)**
- Admin seed (default admin user in dev)

### Admin Test Checklist
- [ ] Go to `/admin` → redirected to login
- [ ] Log in with seeded credentials
- [ ] See dashboard with zero counts
- [ ] Edit site settings → change phone number
- [ ] Visit public site → see updated phone in footer
- [ ] Log out → redirected to login
- [ ] Try `/admin/dashboard` while logged out → redirected

### BDD Scenarios (18)
```gherkin
S1-01: Security headers set on all responses (CSP, X-Frame, XSS, Referrer-Policy)
S1-02: Health check at /health returns 200
S1-03: Homepage renders with title and meta description
S1-04: Header shows nav links (Solutions, Industries, About, Resources, Contact)
S1-05: Mobile hamburger opens full-screen overlay
S1-06: Footer shows 4-column layout with copyright
S1-07: 404 page renders for unknown routes
S1-08: 500 page renders on server error
S1-09: Admin login page renders at /admin/login
S1-10: Admin login with valid credentials → redirect to dashboard
S1-11: Admin login with invalid credentials → error message
S1-12: Admin lockout after 5 failed attempts
S1-13: Unauthenticated access to /admin/* → redirect to login
S1-14: Admin dashboard shows metric cards (page views, submissions, content counts)
S1-15: Admin site settings → edit and save
S1-16: Site settings values appear in public layout (footer phone, email)
S1-17: Admin logout works
S1-18: No "lead" language in any customer-facing text
```

---

## Slice 2: Homepage Content (DB-Driven) + Admin CRUD
**You can test:** Add testimonials, client logos, and stats via admin. See them render on the homepage.

### Ships
- Homepage sections: hero, social proof logos, stats, capabilities, how-it-works, testimonials, case study preview, final CTA
- All homepage content pulled from database
- **Admin: Testimonials CRUD** (quote, name, title, company, photo upload, sort order, active toggle)
- **Admin: Client Logos CRUD** (company name, logo upload, URL, sort order, active toggle)
- **Admin: Stats editing** (via site settings — stat values + labels)
- **Admin: Image upload with ImageSharp** (auto WebP, thumb/medium/large)
- Alpine.js: logo carousel, stats counter animation, testimonial carousel

### Admin Test Checklist
- [ ] Admin → Testimonials → Create new → fill fields + upload photo
- [ ] Admin → Testimonials → See it in list → toggle active off
- [ ] Visit homepage → testimonial carousel shows active testimonials only
- [ ] Admin → Client Logos → Upload 6 logos with company names
- [ ] Homepage → logo bar scrolls with all 6
- [ ] Admin → Site Settings → Change stat values (e.g., "340%" → "400%")
- [ ] Homepage → stats section shows updated values
- [ ] Admin → Testimonials → Reorder via sort order → homepage reflects new order
- [ ] Upload image → verify WebP thumbnail generated in `/images/uploads/`

### BDD Scenarios (22)
```gherkin
S2-01: Hero renders headline, subtext, dual CTAs
S2-02: Hero responsive — stacks on mobile, full-width CTAs
S2-03: Logo carousel auto-scrolls, pauses on hover
S2-04: Logo carousel renders from DB (only active logos)
S2-05: Stats animate counting on scroll (Alpine.js IntersectionObserver)
S2-06: Stats values come from site settings in DB
S2-07: Stats responsive — 2x2 grid on mobile
S2-08: Capabilities grid renders 5 cards with icons
S2-09: How-it-works shows 4-step flow
S2-10: Testimonial carousel renders from DB, auto-advances
S2-11: Testimonial carousel swipeable on mobile
S2-12: Testimonial accessibility (role="region", aria labels)
S2-13: Case study preview cards render (2 latest)
S2-14: Final CTA with dual buttons + trust checkmarks
S2-15: Admin → Create testimonial with all fields
S2-16: Admin → Edit testimonial → public site updates
S2-17: Admin → Delete testimonial → removed from carousel
S2-18: Admin → Upload client logo → appears in logo bar
S2-19: Admin → Toggle logo active/inactive → reflects on homepage
S2-20: Admin → Image upload generates WebP variants (thumb/medium/large)
S2-21: Homepage Organization JSON-LD schema
S2-22: No "lead" language on homepage
```

---

## Slice 3: Contact + Demo + Forms + Admin Submissions View
**You can test:** Submit contact form, submit demo booking. View submissions in admin. Verify Phoenix CRM webhook fires.

### Ships
- **Contact page** — 4-step multi-step form (Alpine.js)
- **Demo page** — booking form + "what you'll see" checklist
- **About page** — story, values, team grid, compliance badges
- `/api/contact` and `/api/demo` endpoints
- FluentValidation + XSS sanitization (HtmlSanitizer)
- Rate limiting (10/min per IP on API endpoints)
- Phoenix CRM webhook on submission
- MailKit confirmation emails
- **Admin: Form Submissions list** (paginated, filterable by type)
- **Admin: Submission detail view** (full data, Phoenix status, timestamp)
- **Admin: Team Members CRUD** (name, title, bio, photo, LinkedIn, sort order)

### Admin Test Checklist
- [ ] Visit `/contact` → fill 4-step form → submit
- [ ] Admin → Submissions → see the submission with all data
- [ ] Check "Sent to Phoenix" column (will show Pending if webhook URL not set)
- [ ] Submit 11 times rapidly from same IP → 11th returns 429
- [ ] Submit with `<script>alert('xss')</script>` in message → verify sanitized in admin view
- [ ] Visit `/demo` → fill booking form → submit → see in admin
- [ ] Admin → Team Members → Add member with photo → see on About page
- [ ] Admin → Team Members → Reorder → About page reflects order
- [ ] Submit contact form with invalid email → see validation error (no page reload)

### BDD Scenarios (24)
```gherkin
S3-01: Contact page renders hero + 4-step form
S3-02: Step 1 collects name, email, phone — validates before advancing
S3-03: Step 2 collects company info
S3-04: Step 3 collects insurance lines + volume
S3-05: Step 4 requires TCPA consent checkbox
S3-06: Form submits via fetch POST to /api/contact
S3-07: FluentValidation returns 400 for invalid data
S3-08: XSS payloads sanitized (HtmlSanitizer strips script tags)
S3-09: Rate limit: 11th request from same IP in 1 min → 429
S3-10: Submission saved to DB with IP, user agent, source page
S3-11: Phoenix CRM webhook fires on submission
S3-12: Confirmation email sent to submitter (MailKit)
S3-13: Internal notification email sent to team
S3-14: Demo page renders booking form
S3-15: Demo form validates date (future only) + time required
S3-16: About page renders story, values, team grid
S3-17: Team members come from DB, ordered by sort order
S3-18: Compliance badges on about page
S3-19: Admin → Submissions list paginated, shows type + date + Phoenix status
S3-20: Admin → Click submission → full detail view
S3-21: Admin → Filter submissions by type (contact/demo)
S3-22: Admin → Team Members CRUD (create, edit, delete, reorder)
S3-23: Contact sidebar shows phone, email, address from site settings
S3-24: No "lead" language on contact/demo/about pages
```

---

## Slice 4: Solutions Pages + Admin Solutions Editor
**You can test:** Create/edit solution pages in admin. See them render at `/solutions/{slug}`. Manage FAQs per solution.

### Ships
- Solutions overview (`/solutions`)
- 5 individual solution pages via dynamic route (`/solutions/{slug}`)
- Solution model: slug, title, overline, hero text, metrics, features, FAQ entries
- Interactive platform diagram (Alpine.js)
- Comparison table component
- FAQ accordion (Alpine.js x-collapse) + Schema.org FAQPage JSON-LD
- **Admin: Solutions CRUD** (create/edit all solution page fields)
- **Admin: Solution FAQ management** (add/edit/reorder/delete per solution)
- **Admin: Solution metrics editor** (4 metrics per solution)

### Admin Test Checklist
- [ ] Admin → Solutions → Create "Data Enrichment" with slug, hero text, 4 metrics
- [ ] Visit `/solutions/data-enrichment` → see rendered page
- [ ] Admin → Add 3 FAQ entries to Data Enrichment
- [ ] Public page → FAQ accordion opens/closes, JSON-LD in page source
- [ ] Admin → Edit hero text → public page updates
- [ ] Admin → Create all 5 solutions → overview page shows all 5 cards
- [ ] Visit `/solutions/invalid-slug` → 404 page
- [ ] Admin → Delete a solution → 404 on its URL, removed from overview

### BDD Scenarios (20)
```gherkin
S4-01: Solutions overview renders breadcrumb + headline
S4-02: Interactive platform diagram with clickable nodes (Alpine.js)
S4-03: 5 alternating solution cards link to /solutions/{slug}
S4-04: Comparison table (The Switchboard vs Typical vs DIY)
S4-05: Solution page renders from DB (hero, metrics, features, FAQ)
S4-06: Key metrics bar shows 4 stats from DB
S4-07: Problem section with 3 pain point cards
S4-08: How-it-works 3-step process
S4-09: FAQ accordion opens/closes (Alpine.js x-collapse)
S4-10: FAQ Schema.org FAQPage JSON-LD in page source
S4-11: Dynamic routing resolves all 5 slugs, invalid → 404
S4-12: Admin → Create solution page with all fields
S4-13: Admin → Edit solution → public page updates
S4-14: Admin → Manage FAQs per solution (add, edit, reorder, delete)
S4-15: Admin → Edit solution metrics
S4-16: Related solutions links on each page
S4-17: Final CTA section on each solution page
S4-18: Unique meta tags per solution (from DB)
S4-19: Solution pages responsive on mobile
S4-20: No "lead" language in solutions content
```

---

## Slice 5: Industry Pages + Admin Industries Editor
**You can test:** Create industry pages in admin. Configure industry-specific stats, enrichment grids, FAQs.

### Ships
- Reusable industry page template (`/industries/{slug}`)
- Auto, Health, Commercial pages
- Industry model: slug, title, stats, enrichment data, process flow, FAQ
- Cross-links to other industries + solutions
- **Admin: Industries CRUD** (all page fields)
- **Admin: Industry FAQ management**
- **Admin: Industry stats + enrichment data grids**

### Admin Test Checklist
- [ ] Admin → Industries → Create "Auto Insurance" with all fields
- [ ] Visit `/industries/auto` → full page renders
- [ ] Admin → Add enrichment data cards (Vehicle, Driver, Risk)
- [ ] Admin → Add 4 FAQ entries → accordion works on public page
- [ ] Admin → Edit stats → public page updates
- [ ] Create all 3 industries → cross-links work between them
- [ ] Invalid slug → 404

### BDD Scenarios (14)
```gherkin
S5-01: Auto Insurance page renders hero + industry stats
S5-02: Enrichment data grid shows 3 data category cards
S5-03: Industry process flow shows 4 customized steps
S5-04: Industry FAQ accordion + JSON-LD
S5-05: Cross-industry links to other verticals
S5-06: Cross-links to relevant solutions
S5-07: Health Insurance page with ACA/Medicare content
S5-08: Commercial Insurance page with business class codes
S5-09: Dynamic routing (auto, health, commercial), invalid → 404
S5-10: Admin → Create/edit industry pages
S5-11: Admin → Manage industry FAQs
S5-12: Admin → Edit industry stats + enrichment data
S5-13: Unique meta tags per industry
S5-14: No "lead" language
```

---

## Slice 6: Blog + Case Studies + Admin Content Editor
**You can test:** Write blog posts with rich text editor in admin. Create case studies. See them on public site. Schedule posts for future.

### Ships
- Blog listing (`/resources/blog`) + post template (`/resources/blog/{slug}`)
- Case study listing (`/resources/case-studies`) + template
- Resources hub (`/resources`) with filtering
- **Admin: Blog post editor** (TipTap rich text, image upload, categories, tags, SEO fields, scheduling)
- **Admin: Case study editor** (structured fields: challenge/solution/results + metrics)
- **Admin: Author management** (name, title, bio, photo, LinkedIn)
- **Admin: FAQ management** (general site FAQs)
- RSS feed (`/resources/blog/rss`)
- Content scheduling background service
- Newsletter signup + subscriber management
- Pagination component

### Admin Test Checklist
- [ ] Admin → Authors → Create author with photo
- [ ] Admin → Blog Posts → New → TipTap editor loads → write content with headings, bold, images
- [ ] Set SEO title + meta description → save as Draft
- [ ] Visit `/resources/blog/{slug}` → 404 (draft)
- [ ] Admin → Change to Published → now visible
- [ ] Admin → Schedule a post for tomorrow → not visible today → visible after scheduled time
- [ ] Admin → Case Studies → Create with challenge/solution/results + 4 metrics
- [ ] Visit `/resources/case-studies/{slug}` → full case study renders
- [ ] Visit `/resources` → filter by type (Blog, Case Studies)
- [ ] Visit `/resources/blog/rss` → valid RSS XML
- [ ] Admin → FAQs → Create general FAQ entries
- [ ] Public newsletter signup → subscriber appears in admin

### BDD Scenarios (22)
```gherkin
S6-01: Resources hub with type filters + search
S6-02: Blog listing shows cards (image, title, excerpt, date, author)
S6-03: Blog post renders rich text body with author bio
S6-04: Blog sidebar (TOC, CTA, related posts)
S6-05: Blog Article JSON-LD schema
S6-06: Case study listing with metric cards
S6-07: Case study full layout (challenge/solution/results + sidebar)
S6-08: Admin → Blog editor with TipTap rich text
S6-09: Admin → Blog image upload in editor → ImageSharp processes
S6-10: Admin → Blog SEO fields (title, description, OG image)
S6-11: Admin → Blog draft vs published workflow
S6-12: Admin → Blog scheduling — publishes at scheduled time
S6-13: Admin → Case study editor with structured fields
S6-14: Admin → Author management CRUD
S6-15: Admin → FAQ management CRUD
S6-16: RSS feed at /resources/blog/rss
S6-17: Newsletter signup → subscriber saved to DB
S6-18: Admin → View subscriber list
S6-19: Pagination on blog/case study listings
S6-20: Related content by tags
S6-21: Blog search (server-side query)
S6-22: No "lead" language in templates
```

---

## Slice 7: Analytics Dashboard + SEO Toolkit + Tools Admin
**You can test:** See page view analytics in admin. Manage redirects. Edit meta tags per page. Run A/B tests. View chat transcripts.

### Ships
- **Admin: Analytics dashboard** (page views, top pages, unique visitors, charts)
- **Admin: SEO meta editor** (per-page title, description, OG image, canonical, noindex)
- **Admin: Redirect manager** (301 redirects from DB)
- **Admin: A/B test manager** (create experiments, assign variants, view results)
- **Admin: Chat transcripts viewer**
- **Admin: Performance dashboard** (CWV per route)
- Chat widget (SignalR — live chat + AI off-hours)
- SEO sitemap.xml generator
- Schema.org builder
- A/B testing middleware
- ROI calculator
- Uptime monitor + status page (`/status`)
- Exit-intent modal
- Security page (`/security`)

### Admin Test Checklist
- [ ] Admin → Analytics → See page view count for today/week/month
- [ ] Admin → Analytics → Top pages list
- [ ] Admin → SEO → Edit meta for `/about` → public page reflects change
- [ ] Admin → Redirects → Add `/old-page` → `/new-page` (301) → test in browser
- [ ] Admin → A/B Tests → Create experiment on homepage CTA text
- [ ] Visit homepage in two browsers → see different variants
- [ ] Admin → A/B Tests → View conversion counts per variant
- [ ] Visit `/status` → uptime status page
- [ ] Visit `/sitemap.xml` → valid XML sitemap
- [ ] Admin → Chat → View transcript of a test conversation

### BDD Scenarios (24)
```gherkin
S7-01: Analytics dashboard shows page views (today/week/month)
S7-02: Top pages list ordered by views
S7-03: Unique visitor count (hashed IPs)
S7-04: SEO meta editor per page
S7-05: Meta changes reflected on public pages
S7-06: Redirect manager — add/edit/delete 301s
S7-07: Redirect middleware processes DB redirects
S7-08: Sitemap.xml auto-generated with all public pages
S7-09: Schema.org builder for Organization, Product, FAQ
S7-10: A/B test middleware assigns variants consistently (cookie-based)
S7-11: A/B test admin shows experiment results + significance
S7-12: Chat widget renders on all public pages
S7-13: Chat — business hours → live chat via SignalR
S7-14: Chat — off hours → AI chatbot via Claude API
S7-15: Chat transcripts viewable in admin
S7-16: ROI calculator with industry-specific models
S7-17: ROI calculator results gated behind email
S7-18: Uptime monitor background service
S7-19: Status page at /status shows service health
S7-20: Exit-intent modal (desktop, once per session)
S7-21: Security page renders compliance info
S7-22: OG image generation per page (ImageSharp)
S7-23: Performance dashboard (CWV per route)
S7-24: No "lead" language in tools UI
```

---

## Slice 8: Polish, Performance, Launch
**You can test:** Full cross-browser QA. Lighthouse scores. Redirects from old site. llms.txt.

### Ships
- Core Web Vitals optimization (LCP < 2.5s, INP < 200ms, CLS < 0.1)
- WCAG 2.1 AA audit + fixes
- Cross-browser testing (Chrome, Firefox, Safari, Edge, iOS Safari, Android Chrome)
- Old site redirect map (301s loaded into redirect manager)
- `llms.txt` for AI crawlers
- Final content review (no "lead" language anywhere)
- Production Railway deployment with PostgreSQL + Redis
- DNS cutover checklist (Namecheap)
- Monitoring + alerting setup

### Launch Checklist
- [ ] Lighthouse Performance ≥ 90 on all page types
- [ ] Lighthouse Accessibility ≥ 95
- [ ] All old URLs redirect to new equivalents
- [ ] Forms submit and webhook to Phoenix CRM
- [ ] Emails send via Amazon SES
- [ ] Admin login works in production
- [ ] `/health` returns 200
- [ ] `/sitemap.xml` includes all pages
- [ ] `robots.txt` allows indexing
- [ ] `llms.txt` at root
- [ ] Google Search Console verified
- [ ] Zero instances of "lead" in customer-facing text

### BDD Scenarios (12)
```gherkin
S8-01: LCP < 2.5s on homepage (3G throttled)
S8-02: INP < 200ms on all interactive pages
S8-03: CLS < 0.1 on all pages
S8-04: All images served as WebP with responsive srcset
S8-05: All pages pass WCAG 2.1 AA (axe-core)
S8-06: Old site URLs → 301 to new equivalents
S8-07: robots.txt allows search engines
S8-08: llms.txt describes site for AI crawlers
S8-09: Production Railway deploy succeeds
S8-10: Production health check passes
S8-11: DNS cutover from Namecheap works
S8-12: Final "lead" language audit — zero instances
```

---

## Summary

| Slice | Public Pages | Admin UI | Tests |
|-------|-------------|----------|-------|
| 1 | Homepage shell, layout, errors | Login, Dashboard, Site Settings | 18 |
| 2 | Homepage (full, DB-driven) | Testimonials, Logos, Stats, Image Upload | 22 |
| 3 | Contact, Demo, About | Submissions, Team Members | 24 |
| 4 | Solutions (overview + 5 pages) | Solutions Editor, Solution FAQs | 20 |
| 5 | Industry pages (3) | Industries Editor, Industry FAQs | 14 |
| 6 | Blog, Case Studies, Resources | Blog Editor (TipTap), Case Study Editor, Authors, FAQs, Subscribers | 22 |
| 7 | Chat, Status, Security, ROI Calc | Analytics Dashboard, SEO Toolkit, Redirects, A/B Tests, Chat Transcripts | 24 |
| 8 | Polish + launch | All admin tested E2E | 12 |
| **Total** | **22 page templates** | **15+ admin sections** | **156** |

### Critical Path
```
Slice 1 → Slice 2 → Slice 3 ──→ Slice 4 → Slice 5 ──→ Slice 8
                                                    ↘
                              Slice 3 → Slice 6 ────→ Slice 8
                                     ↘
                              Slice 3 → Slice 7 ────→ Slice 8
```
Slices 4/5, 6, and 7 can run in parallel after Slice 3.
