# The Switchboard — Enterprise Website Rebuild
# Full Project Scope

**Date:** 2026-03-10
**Project Code:** SB-WEB-2026
**Status:** Scoped — Awaiting Approval
**IDEA Protocol ID:** 5f67656c-a7ce-4ab6-8401-7bd12af614a4

---

## Executive Summary

Complete gut-and-rebuild of theswitchboardmarketing.com from a 2-page placeholder (current grade: **D+**) into an enterprise-grade digital presence that positions The Switchboard as the premier **risk segmentation and insurance intelligence platform** — not a lead gen company.

**Strategic goal:** Every page, component, and interaction should reinforce the narrative: *"We are the intelligence layer that makes insurance customer acquisition predictable, profitable, and compliant."*

---

## 1. IDEA Protocol Summary

| Phase | ID | Output |
|---|---|---|
| **IDEA** | `5f67656c` | Enterprise website rebuild — risk segmentation positioning |
| **INTENT** | `33036490` | Market repositioning + credibility gap closure |
| **AUDIENCE** | `9e58aee3` | Primary: agency decision makers. Secondary: carriers/MGAs. Tertiary: InsurTech partners |
| **RESEARCH** | `14a7a628` | Insurance buyer psychology, competitive landscape, conversion benchmarks |

**Scenario IDs (5 workstreams, 280 BDD scenarios):**
- Information Architecture: `99eec44f`
- Design System: `5fff7713`
- Conversion Infrastructure: `8cf81702`
- SEO & Content: `6c59dd8c`
- Analytics & Trust: `5a7b303a`

---

## 2. Information Architecture & Site Map

### Primary Navigation
```
Logo | Solutions ▾ | Industries ▾ | Resources ▾ | About | Contact | [Request Demo]
```

### Complete Page Inventory (22 pages)

#### Core Pages
| # | Page | URL | Purpose | Priority |
|---|---|---|---|---|
| 1 | **Homepage** | `/` | Hero + value prop + social proof + capabilities overview + CTA | P0 |
| 2 | **About** | `/about` | Company story, mission, team, values, office locations | P0 |
| 3 | **Contact** | `/contact` | Multi-step qualification form + map + phone + chat | P0 |
| 4 | **Request Demo** | `/demo` | Calendly/Cal.com embed + qualification questions | P0 |

#### Solutions Pages (by capability)
| # | Page | URL | Purpose | Priority |
|---|---|---|---|---|
| 5 | **Solutions Overview** | `/solutions` | All capabilities summary with comparison matrix | P0 |
| 6 | **Data Enrichment** | `/solutions/data-enrichment` | Equifax integration, 90GB dataset, real-time enrichment | P1 |
| 7 | **Risk Segmentation** | `/solutions/risk-segmentation` | ML scoring, risk categorization, carrier matching | P1 |
| 8 | **Quality Scoring** | `/solutions/quality-scoring` | Predictive analytics, conversion likelihood, lead grading | P1 |
| 9 | **Performance Analytics** | `/solutions/analytics` | Closed-loop reporting, ROI dashboards, real-time metrics | P1 |
| 10 | **Decision Engine** | `/solutions/decision-engine` | Real-time routing, pricing optimization, buyer matching | P1 |

#### Industry Pages
| # | Page | URL | Purpose | Priority |
|---|---|---|---|---|
| 11 | **Auto Insurance** | `/industries/auto` | Auto-specific risk scoring, carrier integrations, case studies | P1 |
| 12 | **Health Insurance** | `/industries/health` | ACA/Medicare lead intelligence, compliance | P1 |
| 13 | **Commercial Insurance** | `/industries/commercial` | Business insurance risk profiling, class code matching | P1 |
| 14 | **Life Insurance** | `/industries/life` | Life/annuity lead scoring and segmentation | P2 |

#### Resources & Content
| # | Page | URL | Purpose | Priority |
|---|---|---|---|---|
| 15 | **Resources Hub** | `/resources` | Filterable library: blog, case studies, whitepapers, webinars | P0 |
| 16 | **Blog** | `/resources/blog` | SEO content engine, thought leadership, industry news | P1 |
| 17 | **Blog Post** | `/resources/blog/[slug]` | Individual article with author, related posts, CTA | P1 |
| 18 | **Case Studies** | `/resources/case-studies` | Customer success stories with metrics | P0 |
| 19 | **Case Study** | `/resources/case-studies/[slug]` | Individual case study: challenge → solution → results | P0 |

#### Legal & Utility
| # | Page | URL | Purpose | Priority |
|---|---|---|---|---|
| 20 | **Privacy Policy** | `/privacy` | CCPA/GDPR compliant, cookie policy | P0 |
| 21 | **Terms of Service** | `/terms` | Legal terms | P0 |
| 22 | **Security** | `/security` | SOC 2 status, data handling, compliance certifications | P1 |

### Footer Navigation
```
Solutions          Industries          Resources          Company
├─ Data Enrichment  ├─ Auto Insurance    ├─ Blog             ├─ About
├─ Risk Segmentation├─ Health Insurance  ├─ Case Studies      ├─ Careers
├─ Quality Scoring  ├─ Commercial        ├─ Whitepapers      ├─ Contact
├─ Analytics        ├─ Life Insurance    ├─ Webinars         ├─ Security
└─ Decision Engine  └─                   └─                   ├─ Privacy
                                                              └─ Terms
```

### URL Strategy
- All lowercase, hyphenated slugs
- Max 3 levels deep: `/solutions/risk-segmentation`
- Canonical URLs on every page
- 301 redirects from old URLs → new
- Trailing slashes: **no** (Next.js default)

---

## 3. Design System

### 3.1 Brand Tokens

#### Colors
| Token | Value | Usage |
|---|---|---|
| `--navy-900` | `#050b24` | Darkest backgrounds |
| `--navy-800` | `#07102e` | Dark sections |
| `--navy-700` | `#091538` | Dark hover states |
| `--navy-600` | `#0b1a42` | **Primary brand** (current) |
| `--navy-500` | `#415fb4` | Links, accents |
| `--navy-400` | `#677fc3` | Secondary text |
| `--accent-blue` | `#2563eb` | CTAs, interactive elements |
| `--accent-green` | `#059669` | Success, positive metrics |
| `--accent-amber` | `#d97706` | Warnings, highlights |
| `--neutral-50` | `#f9fafb` | Page backgrounds |
| `--neutral-100` | `#f3f4f6` | Card backgrounds |
| `--neutral-600` | `#4b5563` | Body text |
| `--neutral-900` | `#111827` | Headings |

#### Typography
| Token | Font | Weight | Size | Usage |
|---|---|---|---|---|
| `display-xl` | Inter | 800 | 72px / 4.5rem | Hero headlines |
| `display-lg` | Inter | 700 | 60px / 3.75rem | Section headlines |
| `heading-1` | Inter | 700 | 48px / 3rem | Page titles |
| `heading-2` | Inter | 700 | 36px / 2.25rem | Section titles |
| `heading-3` | Inter | 600 | 24px / 1.5rem | Subsection titles |
| `heading-4` | Inter | 600 | 20px / 1.25rem | Card titles |
| `body-lg` | Inter | 400 | 18px / 1.125rem | Lead paragraphs |
| `body` | Inter | 400 | 16px / 1rem | Body text |
| `body-sm` | Inter | 400 | 14px / 0.875rem | Captions, labels |
| `overline` | Inter | 700 | 12px / 0.75rem | Overline labels (uppercase, tracking-wider) |

#### Spacing Scale
```
4px  8px  12px  16px  20px  24px  32px  40px  48px  64px  80px  96px  128px
```

#### Breakpoints
| Name | Width | Target |
|---|---|---|
| `sm` | 640px | Large phones |
| `md` | 768px | Tablets |
| `lg` | 1024px | Small laptops |
| `xl` | 1280px | Desktops |
| `2xl` | 1536px | Large screens |

### 3.2 Component Library

#### Atoms (foundational)
- Button (primary, secondary, ghost, destructive, sizes: sm/md/lg)
- Input (text, email, phone, textarea, select, checkbox, radio)
- Badge / Tag (status, category, industry)
- Icon system (Lucide icons, consistent 24px grid)
- Logo (full, mark only, negative/positive variants)
- Avatar (team member photos, company logos)
- Spinner / Loading state
- Tooltip

#### Molecules (composed)
- Form Field (label + input + error + helper text)
- Nav Link (with dropdown indicator)
- Search Bar (with filter pills)
- Stat Card (number + label + trend arrow)
- Testimonial Card (quote + photo + name + title + company)
- Feature Card (icon + title + description + link)
- Blog Card (image + category + title + excerpt + date)
- Case Study Card (logo + title + metric highlight)
- CTA Block (headline + description + button)
- Breadcrumb (auto-generated from URL path)
- Pagination

#### Organisms (page sections)
- Header / Navigation (desktop + mobile + mega-menu)
- Footer (4-column + newsletter + social + legal)
- Hero Section (variants: homepage, solutions, industry, resource)
- Feature Grid (2-col, 3-col, 4-col layouts)
- Social Proof Bar (client logo carousel + count)
- Stats Counter Section (animated counting numbers)
- Testimonial Carousel (multi-slide, auto-rotate)
- Comparison Table (features vs competitors)
- Platform Diagram (interactive flow visualization)
- Pricing / Package Table (if applicable)
- Team Grid (headshots + bios + LinkedIn links)
- FAQ Accordion (with schema markup)
- Newsletter Signup (inline + modal variants)
- Cookie Consent Banner
- Exit-Intent Modal
- ROI Calculator Widget
- Contact Form (multi-step with progress bar)
- Demo Booking Widget

#### Page Templates
- Homepage Template
- Solutions Page Template
- Industry Page Template
- Resource Hub Template
- Blog Post Template
- Case Study Template
- About Page Template
- Contact Page Template
- Legal Page Template

---

## 4. Conversion & Lead Capture Infrastructure

### 4.1 Multi-Step Qualification Form
```
Step 1: Contact Info     → First Name, Last Name, Email, Phone
Step 2: Company Info     → Company Name, Title/Role, Company Size (dropdown)
Step 3: Needs Assessment → Insurance Lines (multi-select), Current Monthly Lead Volume,
                           Primary Challenge (dropdown), Timeline (dropdown)
Step 4: Confirmation     → Summary + TCPA consent + Submit
```
**Fields feed into CRM** (HubSpot or Salesforce) with lead scoring based on company size + volume + timeline.

### 4.2 Demo Scheduling
- Cal.com or Calendly embed on `/demo`
- Pre-fills from form data
- Calendar shows available 30-min slots
- Confirmation email with Zoom/Meet link
- Reminder 24h and 1h before

### 4.3 Gated Content
- Whitepapers and industry reports behind email gate
- Progressive profiling: first download = email only, subsequent = company + title
- Cookie remembers gated users for 30 days

### 4.4 Chat Widget
- Intercom or Drift integration
- Business hours: live chat
- Off hours: AI chatbot with FAQ responses
- Triggers: 60s on pricing page, 30s on demo page, exit intent

### 4.5 Additional Capture Points
| Location | Mechanism | Data Collected |
|---|---|---|
| Header | "Request Demo" button | Routes to `/demo` |
| Homepage hero | Primary CTA | Routes to `/demo` |
| Blog posts | Inline CTA + sidebar | Email for newsletter |
| Case studies | Bottom CTA | Full qualification form |
| Footer | Newsletter signup | Email + industry segment |
| Exit intent | Modal popup | Email + "What were you looking for?" |
| ROI Calculator | Results gated | Email + company + current metrics |
| Every page | Chat widget | Conversational qualification |

---

## 5. SEO & Content Marketing Infrastructure

### 5.1 Technical SEO
- **Schema.org markup** on every page:
  - `Organization` (homepage)
  - `Product` (solutions pages)
  - `FAQPage` (FAQ sections)
  - `Article` + `BlogPosting` (blog)
  - `BreadcrumbList` (all pages)
  - `HowTo` (process sections)
- **Dynamic sitemap.xml** via Next.js Metadata API
- **robots.txt** with crawl directives
- **Canonical URLs** on every page
- **Open Graph + Twitter Cards** for social sharing
- **hreflang** (if multi-language future)

### 5.2 Content Management
- **Headless CMS**: Sanity.io (recommended) or Contentful
  - Blog posts with rich text, images, code blocks
  - Case studies with structured fields (challenge, solution, results, metrics)
  - Team member profiles
  - Testimonials
  - Client logos
  - FAQ entries
- **Content types in CMS:**
  - Blog Post (title, slug, author, category, tags, body, featured image, SEO fields)
  - Case Study (client name, logo, industry, challenge, solution, results, metrics, testimonial quote)
  - Author (name, bio, headshot, title, LinkedIn)
  - Testimonial (quote, author, title, company, logo, rating)
  - FAQ (question, answer, category)

### 5.3 Performance Targets (Core Web Vitals)
| Metric | Target | Current Estimate |
|---|---|---|
| LCP (Largest Contentful Paint) | < 2.5s | Unknown |
| INP (Interaction to Next Paint) | < 200ms | Unknown |
| CLS (Cumulative Layout Shift) | < 0.1 | Unknown |
| TTFB (Time to First Byte) | < 800ms | Unknown |
| Speed Index | < 3.0s | Unknown |
| Lighthouse Score | > 90 | Unknown |

### 5.4 Image Pipeline
- Next.js `<Image>` with automatic WebP/AVIF
- Responsive `srcset` for all viewport sizes
- Lazy loading below the fold
- Blur-up placeholder for hero images
- CMS images served via CDN (Sanity CDN or Cloudinary)

---

## 6. Analytics, Tracking & Trust Infrastructure

### 6.1 Analytics Stack
| Tool | Purpose |
|---|---|
| **GA4** | Page analytics, user behavior, acquisition |
| **GTM** | Tag management, event firing (already have GTM-K6Q5Q95W) |
| **Hotjar / Clarity** | Heatmaps, session recordings, scroll depth |
| **HubSpot / Salesforce** | CRM, lead tracking, attribution |
| **Sentry** | Error tracking, performance monitoring |

### 6.2 Event Tracking Plan
| Event | Trigger | Properties |
|---|---|---|
| `page_view` | Every page load | page_path, page_title, referrer |
| `cta_click` | Any CTA button | cta_text, cta_location, destination |
| `form_start` | First form field focus | form_name, page_path |
| `form_step` | Multi-step form progression | form_name, step_number, step_name |
| `form_submit` | Form submission | form_name, fields_completed |
| `demo_request` | Demo form submitted | company_size, insurance_lines |
| `content_download` | Gated content downloaded | content_title, content_type |
| `chat_open` | Chat widget opened | page_path, time_on_page |
| `video_play` | Video started | video_title, page_path |
| `scroll_depth` | 25%, 50%, 75%, 100% | depth_percentage, page_path |
| `exit_intent` | Exit intent triggered | page_path, time_on_page |
| `roi_calculator_complete` | Calculator results viewed | inputs, calculated_roi |

### 6.3 Social Proof & Trust Signals

#### Must-Have (P0)
- Client logo carousel (minimum 6 logos, ideally 12+)
- Testimonial rotation (3+ quotes with photos, titles, companies)
- Key metrics counter: "X leads processed", "Y% avg conversion lift", "Z insurance carriers"
- TCPA compliance badge on all forms

#### Should-Have (P1)
- SOC 2 Type II badge (or "in progress" badge)
- Insurance industry association membership badges
- "Data encrypted at rest and in transit" badge
- Team page with headshots, bios, LinkedIn links
- Physical address + phone number in footer (already have)

#### Nice-to-Have (P2)
- Real-time lead counter (live API showing today's processed leads)
- Uptime badge (via UptimeRobot or BetterStack)
- G2/Capterra review widget
- Partner/integration logo grid (Equifax, carriers, etc.)

---

## 7. Technical Architecture

### 7.1 Stack
| Layer | Technology | Rationale |
|---|---|---|
| **Framework** | Next.js 15 (App Router) | SSR/SSG, React Server Components, API routes |
| **Language** | TypeScript (strict mode) | Type safety, maintainability |
| **Styling** | Tailwind CSS 4 + CSS Modules | Utility-first + scoped styles for complex components |
| **CMS** | Sanity.io | Real-time preview, flexible schemas, TypeScript SDK |
| **Forms** | React Hook Form + Zod | Validation, multi-step, performance |
| **Email** | Resend (replace Nodemailer) | Reliable transactional email, templates |
| **Analytics** | GTM + GA4 + Vercel Analytics | Full-stack analytics |
| **Hosting** | Vercel | Edge network, preview deploys, ISR |
| **CDN/Images** | Vercel Image Optimization + Sanity CDN | Automatic format/resize |
| **Monitoring** | Sentry + Vercel Speed Insights | Error + performance monitoring |
| **Chat** | Intercom or Drift | Lead qualification chat |
| **Scheduling** | Cal.com (open source) | Demo booking |
| **CRM** | HubSpot (free tier) or Salesforce | Lead management |

### 7.2 Project Structure
```
apps/
  web/                          ← Marketing website (REBUILD TARGET)
    src/
      app/                      ← Next.js App Router pages
        (marketing)/            ← Marketing pages route group
          page.tsx              ← Homepage
          about/page.tsx
          contact/page.tsx
          demo/page.tsx
          solutions/
            page.tsx
            [slug]/page.tsx
          industries/
            [slug]/page.tsx
          resources/
            page.tsx
            blog/
              page.tsx
              [slug]/page.tsx
            case-studies/
              page.tsx
              [slug]/page.tsx
        api/                    ← API routes
          contact/route.ts
          newsletter/route.ts
          lead-score/route.ts
      components/
        ui/                     ← Atomic design system components
          Button.tsx
          Input.tsx
          Badge.tsx
          ...
        sections/               ← Page section organisms
          Hero.tsx
          FeatureGrid.tsx
          SocialProofBar.tsx
          StatsCounter.tsx
          TestimonialCarousel.tsx
          ...
        layout/                 ← Layout components
          Header.tsx
          Footer.tsx
          MegaMenu.tsx
          MobileNav.tsx
        forms/                  ← Form components
          ContactForm.tsx
          DemoForm.tsx
          NewsletterForm.tsx
          MultiStepForm.tsx
      lib/                      ← Utilities
        sanity/                 ← CMS client + queries
        analytics/              ← Event tracking helpers
        seo/                    ← Schema.org generators
        validation/             ← Zod schemas
      styles/                   ← Global styles + design tokens
      types/                    ← TypeScript types
    public/                     ← Static assets
    sanity/                     ← Sanity Studio (embedded)
      schemas/                  ← Content schemas

  data-ingestion/               ← Keep as-is (backend)
packages/                       ← Keep as-is (backend services)
```

### 7.3 Monorepo Strategy
- **Website rebuild happens in `apps/web/`** — full replacement
- **Backend packages (`packages/*`) remain untouched** — they power the platform
- **`apps/data-ingestion/` remains untouched** — Equifax pipeline
- Shared TypeScript config at root level
- Turbo pipeline updated for new CMS build step

---

## 8. Phased Delivery Plan

### Phase 0: Foundation (Week 1)
**Goal:** Project scaffolding, design system, dev environment
- [ ] Initialize new `apps/web/` with Next.js 15 + TypeScript strict
- [ ] Set up Tailwind 4 with design token config
- [ ] Build atomic component library (Button, Input, Badge, etc.)
- [ ] Set up Sanity.io project + content schemas
- [ ] Configure Vercel preview deployments
- [ ] Set up Sentry error tracking
- [ ] Configure ESLint + Prettier + Husky

**Deliverable:** Empty site with design system Storybook/preview

### Phase 1: Core Pages (Weeks 2-3)
**Goal:** The 4 pages that matter most for conversion
- [ ] Homepage (hero + features + social proof + stats + CTA)
- [ ] About page (story + team + values)
- [ ] Contact page (multi-step qualification form)
- [ ] Demo request page (Cal.com embed)
- [ ] Header with mega-menu navigation
- [ ] Footer with full site map
- [ ] SEO foundation (meta tags, schema.org, sitemap, robots.txt)
- [ ] GTM + GA4 event tracking setup
- [ ] Fix XSS vulnerability in contact API

**Deliverable:** Live core site with conversion infrastructure

### Phase 2: Solutions & Industries (Weeks 4-5)
**Goal:** Product depth for buyer education
- [ ] Solutions overview page
- [ ] 5 individual solution pages (data enrichment, risk segmentation, quality scoring, analytics, decision engine)
- [ ] 3 industry pages (auto, health, commercial)
- [ ] Platform diagram (interactive)
- [ ] Comparison tables
- [ ] FAQ sections with schema markup

**Deliverable:** Full product/industry content live

### Phase 3: Content & Resources (Weeks 6-7)
**Goal:** Content marketing engine
- [ ] Resources hub with filtering
- [ ] Blog listing + individual post template
- [ ] Case study listing + individual template
- [ ] Sanity Studio configured for content team
- [ ] Gated content system (whitepaper downloads)
- [ ] Newsletter signup with segmentation
- [ ] Author profiles
- [ ] Related content engine

**Deliverable:** Content publishing pipeline operational

### Phase 4: Trust & Optimization (Week 8)
**Goal:** Social proof + conversion optimization
- [ ] Client logo carousel (populate with real logos)
- [ ] Testimonial carousel (populate with real quotes)
- [ ] Stats counter section (real metrics)
- [ ] Team page with headshots
- [ ] Security/compliance page
- [ ] Chat widget integration
- [ ] Exit-intent popup
- [ ] ROI calculator widget
- [ ] Cookie consent management
- [ ] Core Web Vitals optimization pass
- [ ] Accessibility audit (WCAG 2.1 AA)
- [ ] Cross-browser testing
- [ ] 301 redirects from old URLs

**Deliverable:** Enterprise-grade site fully operational

### Phase 5: Post-Launch (Ongoing)
- [ ] A/B testing framework activation
- [ ] Heatmap analysis and UX iteration
- [ ] Blog content calendar (2 posts/week)
- [ ] Case study pipeline (1/month)
- [ ] SEO monitoring and keyword expansion
- [ ] Conversion rate optimization sprints
- [ ] Life insurance industry page (P2)

---

## 9. Success Metrics

| Metric | Current | 90-Day Target | Notes |
|---|---|---|---|
| Pages indexed | ~6 | 30+ | Site map expansion |
| Organic traffic | Unknown | +200% | SEO infrastructure |
| Avg session duration | Unknown | > 2 min | Content depth |
| Pages per session | ~1.5 | > 3.5 | Navigation + content |
| Form submission rate | Unknown | > 3% | Multi-step forms |
| Demo requests/month | Unknown | 20+ | Demo page + CTAs |
| Bounce rate | Unknown | < 45% | Better content + UX |
| Lighthouse score | Unknown | > 90 | Performance optimization |
| Core Web Vitals | Unknown | All green | LCP/INP/CLS targets |

---

## 10. Risks & Dependencies

| Risk | Impact | Mitigation |
|---|---|---|
| Content not ready (testimonials, case studies, team photos) | High | Start content collection in Phase 0, use placeholders initially |
| Sanity CMS learning curve for content team | Medium | Provide training, document content workflows |
| CRM integration complexity | Medium | Start with HubSpot free tier, validate form→CRM pipeline early |
| Client logo/testimonial permissions | Medium | Get written consent, start outreach week 1 |
| Performance regression from CMS + analytics scripts | Low | Performance budget enforced in CI, Lighthouse checks |
| SEO migration (301 redirects) | Low | Minimal — current site has negligible SEO equity |

---

## Approval

- [ ] **Project scope approved** — Proceed to Phase 0
- [ ] **Content collection started** — Client logos, testimonials, team photos, case study data
- [ ] **CMS selection confirmed** — Sanity.io (recommended)
- [ ] **CRM selection confirmed** — HubSpot or Salesforce
- [ ] **Chat widget selection confirmed** — Intercom or Drift
- [ ] **Design mockups approved** — Before Phase 1 build begins

---

*Generated via Memento IDEA Protocol. 280 BDD scenarios persisted across 5 workstreams.*
*Audit report: `/WEBSITE_AUDIT_REPORT_CARD.md`*
