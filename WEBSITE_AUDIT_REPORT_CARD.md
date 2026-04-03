# The Switchboard Website - Audit Report Card
**Date:** 2026-03-10
**URL:** https://theswitchboardmarketing.com
**Repo:** TheSBMarketing-main (Next.js monorepo)

---

## Overall Grade: D+ (1.5 / 4.0)

A functional placeholder that gets the basics right technically but fails to serve as a credible enterprise sales tool. This is a "we have a website" site, not a "this website sells for us" site.

---

## Category Breakdown

| Category | Grade | Score | Notes |
|---|---|---|---|
| **Strategic Positioning** | **C-** | 1.7 | Correctly avoids "lead gen" language, uses "risk segmentation" framing. But messaging is vague, undifferentiated, and reads like a template. No unique value proposition that separates from EverQuote or MediaAlpha. |
| **Information Architecture** | **F** | 0.5 | 2 real pages (Home + Contact). No Solutions, About, Industries, Resources, or Case Studies. Enterprise buyers research 6-10 pages before contacting sales. |
| **Content Depth** | **F** | 0.3 | ~200 words of actual content on homepage. Zero case studies, zero metrics, zero proof. "Profitable results" and "optimal performance" are empty promises without numbers. |
| **Social Proof & Trust** | **F** | 0.0 | Zero: no testimonials, no client logos, no case studies, no stats, no team page, no compliance badges, no security certifications. Single biggest killer for enterprise conversion. |
| **Lead Capture & Conversion** | **D** | 1.0 | One basic contact form (name, email, message). No demo scheduling, no chatbot, no gated content, no multi-step qualification. Form doesn't capture company name, title, or size. |
| **SEO** | **D-** | 0.7 | One meta title + description. No schema markup, no sitemap config, no blog, no keyword strategy, no internal linking. Essentially invisible to organic search. |
| **Design & Visual Quality** | **B-** | 2.7 | Strongest area. Clean navy palette, proper Tailwind config with custom color scale, responsive mobile menu, consistent spacing. But generic — looks like any B2B SaaS template. |
| **Technical Foundation** | **B** | 3.0 | Next.js App Router, TypeScript, Tailwind, proper component structure, GTM integrated, Vercel deployment. Monorepo with Turborepo is solid. Backend packages show engineering maturity. |
| **Code Quality** | **B-** | 2.7 | Clean, readable components. Proper form handling with loading/error states. TCPA consent. But: no reusable component library, inline SVGs everywhere, no design tokens beyond colors, `!important` overrides. |
| **Performance** | **C+** | 2.3 | Next.js Image optimization present. But no lazy loading strategy, no code splitting, no font optimization beyond Inter, no Core Web Vitals monitoring. |
| **Accessibility** | **D** | 1.0 | Mobile menu has `aria-label`. No skip-to-content, no ARIA landmarks, no focus management, no contrast verification, no `aria-describedby` for form errors. |
| **Security** | **C** | 2.0 | Contact API has basic validation. No rate limiting, no CSRF, no honeypot for spam. XSS risk in email template (user message interpolated into HTML without sanitization). |

---

## Top 5 Critical Problems

### 1. Zero Credibility Signals
An enterprise insurance buyer landing on this site has no reason to trust you. No logos, no numbers, no proof, no team. They'll bounce in 8 seconds.

### 2. No Buyer Journey
Home → Contact is a 2-step dead end. Enterprise buyers need 5-12 touchpoints before converting. No resources, no case studies, no "how it works" deep dive.

### 3. Generic Messaging
"Transform Insurance Customer Acquisition" could be anyone's tagline. Nothing communicates what makes The Switchboard's risk segmentation *different* from every other data enrichment vendor.

### 4. No Lead Qualification
Contact form doesn't capture company, title, size, or use case. Every lead arrives with zero context. Sales wastes time qualifying instead of closing.

### 5. XSS Vulnerability
`apps/web/src/app/api/contact/route.ts:59` — user-submitted `message` is interpolated directly into HTML email without sanitization. Attacker could inject script tags via contact form.

---

## What's Actually Good

- **Backend monorepo is impressive** — Decision engine, analytics, partner API, duplicate detection packages show real platform capability. Website doesn't reflect it.
- **Strategic positioning direction is correct** — "Risk segmentation" instead of "lead gen" is the right move. Needs depth and specificity.
- **Technical stack is solid** — Next.js + TypeScript + Tailwind + Turborepo is the right foundation.
- **TCPA compliance checkbox** — Shows awareness of regulatory requirements (important for insurance).

---

## Current Site Inventory

### Pages
| Page | Path | Purpose | Status |
|---|---|---|---|
| Homepage | `/` | Hero + Features + Platform Diagram + CTA | Thin content, generic |
| Contact | `/contact` | Contact form (4 fields + consent) | Functional, under-qualified |
| Login | `/login` | Client portal access | Internal use |
| Dashboard | `/dashboard` | Lead analytics dashboard | Internal use |
| Privacy Policy | `/privacy-policy` | Legal compliance | Standard |
| Terms of Service | `/terms-of-service` | Legal compliance | Standard |

### Components
| Component | File | Notes |
|---|---|---|
| Header | `src/components/Header.tsx` | 2 nav links + Login CTA, mobile hamburger |
| Footer | `src/components/Footer.tsx` | 3-col: logo/tagline, quick links, contact info |

### API Routes
| Route | File | Notes |
|---|---|---|
| Contact Form | `src/app/api/contact/route.ts` | Nodemailer → SMTP, basic validation, **XSS risk** |

### Design System
| Token | Value | Notes |
|---|---|---|
| Primary Color | `#0b1a42` (navy) | Full 50-900 scale defined |
| Font | Inter (Google Fonts) | Single font, no heading/body differentiation |
| Button Primary | Navy bg, white text, rounded-lg | `.btn-primary` utility class |
| Button Secondary | Navy border, navy text, rounded-lg | `.btn-secondary` utility class |
| Section Spacing | `px-6 md:px-12 lg:px-24 py-16 md:py-24` | `.section-padding` utility |
| Container | `max-w-7xl mx-auto` | `.container-custom` utility |

### Backend Packages (Not customer-facing, but show platform depth)
- `api-gateway` — Lead intake, auth, JWT, rate limiting, FastBusinessQuote mapper
- `decision-engine` — ML scoring and routing
- `analytics` — ClickHouse metrics and reporting
- `database` — PostgreSQL + Prisma, 3 migrations, seed data
- `duplicate-detection` — Lead deduplication service
- `monitoring` — Health aggregation, error logging, metrics middleware
- `partner-api` — Partner integration service
- `config` — Shared environment config
- `data-ingestion` — Equifax SFTP ingestion + batch loader

---

## Bottom Line

The backend platform is a **B+**. The website selling that platform is a **D+**. There's a massive gap between what The Switchboard *is* and what the website *communicates*. The rebuild isn't cosmetic — it's the difference between looking like a side project and looking like a platform that handles 10K+ leads/day.
