# The Switchboard — Website Build Slice Plan
**Date:** 2026-03-11
**Status:** Ready for Build
**IDEA Protocol ID:** 5f67656c-a7ce-4ab6-8401-7bd12af614a4

---

## Slice Philosophy

Each slice is a **vertical, shippable increment**. Every slice ends with something deployable to Railway. No slice exceeds 1,000 lines of net-new code (per the 94% success rate pattern). Slices are sequenced so the site builds credibility progressively — the most conversion-critical elements ship first.

**Language rule:** The word "lead" does NOT appear in any customer-facing copy. Use: prospects, opportunities, customers, shoppers, risk-scored opportunities.

**Stack rule:** C# + ASP.NET Core 9. Self-owned everything. No 3rd-party SaaS dependencies except Railway (hosting), SMTP relay (deliverability), and DNS.

**CRM rule:** Phoenix CRM already exists. Form submissions route to Phoenix via webhook/API. Do NOT build CRM features in this project.

---

## Tech Stack

| Layer | Technology | Notes |
|---|---|---|
| **Framework** | ASP.NET Core 9 (Razor Pages) | SSR, SEO-friendly, fast |
| **Language** | C# 13 | Consistent with Phoenix CRM + GBC |
| **Styling** | Tailwind CSS 4 | CLI build pipeline |
| **Interactivity** | Alpine.js | 15KB, no build step, covers carousels/menus/accordions/counters |
| **Database** | PostgreSQL 17 | Content, forms, analytics, images — all on Railway |
| **ORM** | EF Core 9 | Migrations, seed data, LINQ |
| **Hosting** | Railway | Docker container, auto-deploy from Git |
| **Email** | MailKit + SMTP relay | Amazon SES or Postmark for deliverability |
| **Image Pipeline** | ImageSharp | Server-side resize/crop/WebP/AVIF, responsive srcset |
| **Validation** | FluentValidation | Server-side, type-safe |
| **Auth (admin)** | ASP.NET Core Identity | Cookie auth for admin panel |
| **Caching** | Redis on Railway | Page cache, session, rate limiting |
| **Real-time** | SignalR | Live chat, real-time visitor count |
| **Logging** | Serilog + Seq | Structured logging, error tracking |
| **Rich Text** | TipTap (admin) | OSS rich text editor for blog/case study content |
| **CRM Integration** | Phoenix CRM API | Webhook on form submit → Phoenix |

### Self-Built Systems (No 3rd Party)

| System | Replaces | Details |
|---|---|---|
| **Admin Panel** | Sanity.io | Razor Pages admin area. Manage: blog posts, case studies, authors, testimonials, FAQs, client logos, team members, page meta |
| **Booking Widget** | Cal.com | Calendar UI, available slots from DB, email confirmations, timezone handling, Google Calendar sync |
| **Chat Widget** | Intercom/Drift | SignalR WebSocket. Business hours = live chat. Off hours = AI chatbot (Claude API). Transcripts in DB |
| **Analytics Engine** | GA4 + GTM | First-party tracking. Page views, events, scroll depth, referrers. PostgreSQL storage. Admin dashboard. No cookie consent needed |
| **Email Lists** | Mailchimp | Subscribers table in DB. Batch send via MailKit. Unsubscribe handling. Segment by industry |
| **Image CDN** | Cloudinary | ImageSharp pipeline. Upload → auto-resize → WebP/AVIF → serve with cache headers |
| **A/B Testing** | Optimizely | Middleware assigns variants. Track conversions. Statistical significance in admin |
| **Uptime Monitor** | UptimeRobot | Background service pings endpoints. Status page at `/status`. Email alerts on downtime |
| **SEO Toolkit** | Yoast/Ahrefs | Sitemap generator, meta editor, Schema.org builder, redirect manager, broken link checker |
| **Form Builder** | Typeform | Admin UI to define multi-step forms. Validation rules. Webhook to Phoenix CRM |
| **OG Image Generator** | Vercel OG | ImageSharp renders branded 1200x630 images per page. Blog title + author + branding |
| **Performance Dashboard** | Vercel Speed Insights | Client JS reports LCP/INP/CLS to server. Admin dashboard with per-route metrics |
| **Content Scheduling** | WordPress | Blog posts with "publish at" datetime. Background service publishes on schedule |
| **ROI Calculator** | Spreadsheets | Industry-specific models. Configurable in admin. PDF export. Results stored in DB |

---

## Project Structure

```
TheSwitchboardWeb/
  src/
    TheSwitchboard.Web/              ← Main web application
      Pages/                          ← Razor Pages
        Index.cshtml                  ← Homepage
        About.cshtml
        Contact.cshtml
        Demo.cshtml
        Solutions/
          Index.cshtml                ← Solutions overview
          Detail.cshtml               ← Individual solution (route: /solutions/{slug})
        Industries/
          Detail.cshtml               ← Industry page (route: /industries/{slug})
        Resources/
          Index.cshtml                ← Resources hub
          Blog/
            Index.cshtml              ← Blog listing
            Detail.cshtml             ← Blog post (route: /resources/blog/{slug})
          CaseStudies/
            Index.cshtml              ← Case studies listing
            Detail.cshtml             ← Case study (route: /resources/case-studies/{slug})
        Security.cshtml
        Privacy.cshtml
        Terms.cshtml
        Status.cshtml                 ← Public uptime/status page
        Error/
          404.cshtml
          500.cshtml
      Pages/Admin/                    ← Admin panel (auth required)
        Dashboard.cshtml
        Content/
          BlogPosts.cshtml
          CaseStudies.cshtml
          Testimonials.cshtml
          FAQs.cshtml
          TeamMembers.cshtml
          ClientLogos.cshtml
        Forms/
          Submissions.cshtml
          FormBuilder.cshtml
        Analytics/
          Overview.cshtml
          PageViews.cshtml
          Events.cshtml
          Heatmaps.cshtml
        SEO/
          MetaEditor.cshtml
          Redirects.cshtml
          Sitemap.cshtml
          SchemaBuilder.cshtml
        Settings/
          Booking.cshtml
          Email.cshtml
          ABTests.cshtml
          Performance.cshtml
      Components/                     ← Razor view components
        UI/
          Button.razor
          Input.razor
          Badge.razor
          Icon.razor
          Logo.razor
          Spinner.razor
          Tooltip.razor
        Molecules/
          FormField.razor
          NavLink.razor
          StatCard.razor
          FeatureCard.razor
          CTABlock.razor
          Breadcrumb.razor
          Pagination.razor
        Sections/
          Hero.razor
          SocialProofBar.razor
          StatsCounter.razor
          FeatureGrid.razor
          ProcessFlow.razor
          TestimonialCarousel.razor
          CaseStudyPreview.razor
          FAQAccordion.razor
          ComparisonTable.razor
          IndustryCards.razor
          FinalCTA.razor
          NewsletterSignup.razor
        Layout/
          Header.razor
          MegaMenu.razor
          MobileNav.razor
          Footer.razor
          CookieConsent.razor
          ChatWidget.razor
          ExitIntentModal.razor
      wwwroot/
        css/                          ← Tailwind output
        js/                           ← Alpine.js + custom scripts
        images/                       ← Static images
        fonts/                        ← Inter font files (self-hosted)
      Services/
        ContentService.cs             ← Blog, case study, FAQ CRUD
        ImageService.cs               ← ImageSharp pipeline
        EmailService.cs               ← MailKit sending
        AnalyticsService.cs           ← First-party event tracking
        BookingService.cs             ← Scheduling widget backend
        ChatService.cs                ← SignalR chat hub
        SeoService.cs                 ← Sitemap, schema, redirects
        FormService.cs                ← Form builder + submissions
        RoiCalculatorService.cs       ← ROI calculation engine
        UptimeService.cs              ← Background health checks
        ABTestService.cs              ← Variant assignment + tracking
        OgImageService.cs             ← Dynamic OG image generation
        PhoenixCrmService.cs          ← Webhook to Phoenix CRM
        PerformanceService.cs         ← CWV collection + reporting
      Models/
        Content/
          BlogPost.cs
          CaseStudy.cs
          Author.cs
          Testimonial.cs
          FAQ.cs
          ClientLogo.cs
          TeamMember.cs
          Page.cs                     ← SEO meta per page
        Forms/
          FormDefinition.cs
          FormField.cs
          FormSubmission.cs
        Analytics/
          PageView.cs
          Event.cs
          ScrollDepth.cs
          ClickHeatmap.cs
        Booking/
          TimeSlot.cs
          Appointment.cs
        Chat/
          ChatSession.cs
          ChatMessage.cs
        Email/
          Subscriber.cs
          EmailTemplate.cs
        SEO/
          Redirect.cs
          SchemaMarkup.cs
        ABTest/
          Experiment.cs
          Variant.cs
          Conversion.cs
        Performance/
          WebVitalMetric.cs
        Site/
          SiteSettings.cs
      Data/
        AppDbContext.cs
        Migrations/
      Middleware/
        AnalyticsMiddleware.cs        ← Captures page views server-side
        RedirectMiddleware.cs         ← 301 redirects from DB
        ABTestMiddleware.cs           ← Variant assignment
        RateLimitMiddleware.cs        ← Rate limiting for forms/API
        SecurityHeadersMiddleware.cs  ← HSTS, CSP, X-Frame-Options
      Hubs/
        ChatHub.cs                    ← SignalR chat
        LiveVisitorHub.cs             ← Real-time visitor count
      BackgroundServices/
        ContentSchedulerService.cs    ← Publish scheduled posts
        UptimeMonitorService.cs       ← Health check pings
        EmailBatchService.cs          ← Newsletter sending
        AnalyticsAggregationService.cs← Roll up analytics data
    TheSwitchboard.Web.Tests/         ← xUnit + bUnit tests
  Dockerfile
  docker-compose.yml                  ← Local dev (PostgreSQL + Redis)
  railway.toml
  tailwind.config.js
  package.json                        ← Tailwind CLI only
```

---

## Slice 1: Foundation & Design System
**Goal:** Scaffold the ASP.NET Core 9 project, establish every design token, build the component library. Ship an empty site with working navigation to Railway.

### What Ships
- ASP.NET Core 9 Razor Pages project
- Tailwind CSS 4 with full design token config
- Inter font self-hosted (no Google Fonts dependency)
- All UI components (Button, Input, Badge, Icon, Logo, Spinner, Tooltip)
- All molecule components (FormField, NavLink, StatCard, FeatureCard, CTABlock, Breadcrumb)
- Header with mega-menu (Alpine.js for interactions)
- Footer (4-column + newsletter + compliance badges)
- Mobile nav (full-screen overlay with accordion sub-menus)
- Empty homepage shell with proper `<head>`, meta tags
- Dockerfile + Railway deployment
- docker-compose.yml for local dev (PostgreSQL + Redis)
- EF Core DbContext with initial migration
- Serilog structured logging
- Security headers middleware (HSTS, CSP, X-Frame-Options)

### Design Tokens
```
Colors:
  --navy-900: #050b24    --navy-800: #07102e    --navy-700: #091538
  --navy-600: #0b1a42    --navy-500: #415fb4    --navy-400: #677fc3
  --accent-blue: #2563eb --accent-green: #059669 --accent-amber: #d97706
  --neutral-50: #f9fafb  --neutral-100: #f3f4f6 --neutral-600: #4b5563
  --neutral-900: #111827

Typography: Inter self-hosted (800/700/600/400) — display-xl through body-sm
Spacing: 4 8 12 16 20 24 32 40 48 64 80 96 128
Breakpoints: sm:640 md:768 lg:1024 xl:1280 2xl:1536
```

### BDD Scenarios (34)

```gherkin
# --- Atoms ---

Scenario: S1-01 — Button renders all variants
  Given the design system is loaded
  When I render a Button with variant "primary"
  Then it displays with accent-blue background and white text
  And it has rounded-lg corners and proper padding for size "md"

Scenario: S1-02 — Button sizes render correctly
  Given the design system is loaded
  When I render Button with size "sm", "md", and "lg"
  Then each has the correct padding, font-size, and min-height

Scenario: S1-03 — Button disabled state
  Given a Button with disabled=true
  Then it has opacity-50 and pointer-events-none
  And it does not trigger form submission

Scenario: S1-04 — Button loading state
  Given a Button with loading=true
  Then it shows a Spinner replacing the label text
  And it is disabled during loading

Scenario: S1-05 — Input renders with label and validation
  Given a FormField with label "Email" and type "email"
  When I submit with an invalid email
  Then the server-side validation error appears below the input
  And the input border changes to red

Scenario: S1-06 — Input variants render correctly
  Given Input components for text, email, phone, textarea, select
  Then each renders with correct input type and styling

Scenario: S1-07 — Badge renders status variants
  Given Badge with variant "success", "warning", "info", "neutral"
  Then each displays correct background color and text

Scenario: S1-08 — Icon system uses Lucide consistently
  Given the Icon component with name "shield"
  Then it renders the Lucide SVG at 24px
  And it accepts a custom size parameter

Scenario: S1-09 — Logo renders full and mark variants
  Given Logo with variant "full"
  Then it shows the wordmark + icon
  When variant is "mark"
  Then it shows icon only

Scenario: S1-10 — Tooltip appears on hover
  Given a Tooltip wrapping a Button (Alpine.js x-data)
  When I hover the Button
  Then the tooltip text appears after 200ms delay
  When I move the cursor away
  Then the tooltip disappears

# --- Molecules ---

Scenario: S1-11 — StatCard displays number and label
  Given a StatCard with value="10M+" and label="Prospects Processed"
  Then it renders the value in display-lg font
  And the label in body-sm below

Scenario: S1-12 — FeatureCard renders icon, title, description, link
  Given a FeatureCard with icon="database" title="Data Enrichment"
  Then it shows the icon, heading-4 title, body description
  And a "Learn More" link at the bottom

Scenario: S1-13 — CTABlock renders headline, description, button
  Given a CTABlock with headline and primary action
  Then it renders centered text with a primary Button below

Scenario: S1-14 — Breadcrumb generates from current path
  Given the current path is "/solutions/risk-segmentation"
  Then Breadcrumb renders "Home > Solutions > Risk Segmentation"
  And each segment is a clickable link except the last

Scenario: S1-15 — NavLink renders with dropdown indicator
  Given a NavLink with hasDropdown=true
  Then it shows a chevron-down icon after the label

# --- Header ---

Scenario: S1-16 — Header renders desktop navigation
  Given viewport width >= 1024px
  Then the header shows Logo, nav links, and "Request Demo" button
  And nav links include: Solutions, Industries, Resources, About, Contact

Scenario: S1-17 — Mega menu opens on hover (desktop)
  Given viewport width >= 1024px
  When I hover "Solutions" in the nav
  Then the mega menu panel opens (Alpine.js x-show with transition)
  And it shows all 5 solution links with icons and descriptions
  And a featured resource card on the right

Scenario: S1-18 — Mega menu closes on mouse leave
  Given the Solutions mega menu is open
  When I move the cursor outside the menu
  Then it closes after 200ms delay

Scenario: S1-19 — Mega menu keyboard navigation
  Given focus is on "Solutions" nav link
  When I press Enter or Space
  Then the mega menu opens
  When I press Escape
  Then it closes and focus returns to the trigger

Scenario: S1-20 — Header becomes sticky on scroll
  Given the page is at the top
  When I scroll down past 100px
  Then the header gets position:sticky with a shadow (Alpine.js @scroll.window)

Scenario: S1-21 — Mobile hamburger menu
  Given viewport width < 1024px
  Then the header shows Logo and hamburger icon
  When I tap the hamburger
  Then a full-screen overlay opens with nav links
  And "Request a Demo" is a full-width button at the bottom
  And phone number (323) 471-6020 is visible

Scenario: S1-22 — Mobile nav accordion sub-menus
  Given the mobile menu is open
  When I tap "Solutions"
  Then the sub-menu expands (Alpine.js x-collapse)
  When I tap "Industries"
  Then Solutions collapses and Industries expands

# --- Footer ---

Scenario: S1-23 — Footer renders 4-column layout (desktop)
  Given viewport width >= 1024px
  Then footer shows: Brand/social, Solutions, Resources+Industries, Company+Contact
  And newsletter signup field with Subscribe button
  And compliance badges (SOC 2, TCPA, Encrypted)
  And copyright "2026 The Switchboard"

Scenario: S1-24 — Footer mobile accordion
  Given viewport width < 768px
  Then footer columns collapse into accordion sections
  And click-to-call button is prominent

Scenario: S1-25 — Newsletter signup in footer
  Given the footer newsletter form
  When I enter a valid email and submit
  Then the form posts to /api/newsletter and shows success message
  When I enter an invalid email
  Then server-side FluentValidation returns error

# --- Infrastructure ---

Scenario: S1-26 — Tailwind tokens match design system
  Given the tailwind.config.js
  Then all color tokens from the design system are defined
  And all font sizes and weights match the typography scale
  And all breakpoints are configured

Scenario: S1-27 — Inter font is self-hosted
  Given the wwwroot/fonts directory
  Then Inter woff2 files exist for weights 400, 600, 700, 800
  And @font-face declarations use font-display: swap
  And no requests to fonts.googleapis.com

Scenario: S1-28 — Razor Pages structure
  Given the Pages directory
  Then Index.cshtml exists as homepage
  And _Layout.cshtml includes Header and Footer components
  And _ViewStart.cshtml references the layout

Scenario: S1-29 — Docker builds and runs
  Given the Dockerfile
  When I run docker build
  Then it produces a runnable container
  And the site is accessible on port 8080

Scenario: S1-30 — Railway deployment works
  Given a push to the main branch
  Then Railway auto-deploys via Dockerfile
  And the site is accessible at the Railway URL

Scenario: S1-31 — PostgreSQL connection works
  Given the docker-compose with PostgreSQL
  When the app starts
  Then EF Core connects and runs migrations
  And health check at /health returns 200

Scenario: S1-32 — Security headers are set
  Given any page response
  Then headers include: Strict-Transport-Security, X-Content-Type-Options,
  X-Frame-Options, Content-Security-Policy, Referrer-Policy

Scenario: S1-33 — Serilog structured logging works
  Given the application starts
  Then Serilog writes structured JSON logs
  And request logging captures method, path, status, duration

Scenario: S1-34 — No customer-facing text contains "lead" or "leads"
  Given all Razor views and components
  When I search for the word "lead" in user-visible strings
  Then zero matches are found
```

### File Inventory (~35 files)
```
TheSwitchboardWeb/
  src/TheSwitchboard.Web/
    Program.cs
    Pages/_Layout.cshtml, _ViewStart.cshtml, _ViewImports.cshtml
    Pages/Index.cshtml + Index.cshtml.cs
    Components/UI/ (8 files)
    Components/Molecules/ (6 files)
    Components/Layout/ (4 files: Header, MegaMenu, MobileNav, Footer)
    wwwroot/css/site.css (Tailwind output)
    wwwroot/js/app.js (Alpine.js init)
    wwwroot/fonts/inter-*.woff2 (4 files)
    Data/AppDbContext.cs
    Models/Email/Subscriber.cs
    Middleware/SecurityHeadersMiddleware.cs
    appsettings.json, appsettings.Development.json
  Dockerfile
  docker-compose.yml
  tailwind.config.js
  package.json
  railway.toml
```

---

## Slice 2: Homepage (Full Build)
**Goal:** Build the most important page. Every section. This is the page that converts or kills.

### What Ships
- Hero section (navy bg, headline, subtext, dual CTAs, hero visualization)
- Social proof logo bar (auto-scrolling carousel via Alpine.js)
- Stats counter section (animated counting on scroll via Alpine.js IntersectionObserver)
- Platform capabilities grid (5 feature cards)
- "How It Works" flow diagram (4-step process)
- Industries served section (3 industry cards)
- Testimonial carousel (Alpine.js, 5 slides, auto-advance, swipe)
- Case study preview section (2 featured cards)
- Final CTA section (navy bg, dual buttons, trust checkmarks)

### BDD Scenarios (28)

```gherkin
Scenario: S2-01 — Hero renders with correct content
  Given the homepage loads
  Then I see overline "INSURANCE INTELLIGENCE PLATFORM"
  And headline "The Intelligence Layer Behind Profitable Insurance Growth"
  And subtext describing risk-scored, conversion-predicted opportunities
  And primary CTA "Request a Demo" linking to /demo
  And secondary CTA "See How It Works" scrolling to that section
  And trust line "Trusted by 50+ insurance agencies processing 10,000+ prospects daily"

Scenario: S2-02 — Hero responsive layout
  Given viewport < 1024px
  Then the hero image stacks below the text
  And CTAs become full-width buttons

Scenario: S2-03 — Hero image loads performantly
  Given the hero section
  Then the hero visual has eager loading (not lazy)
  And LCP for this element is < 2.5s

Scenario: S2-04 — Logo carousel renders and auto-scrolls
  Given the social proof section
  Then I see "Trusted by leading insurance organizations"
  And a horizontally scrolling carousel of client logos (Alpine.js)
  And the carousel auto-scrolls continuously
  And it pauses on hover

Scenario: S2-05 — Logo carousel accessibility
  Given the logo carousel
  Then it has aria-label "Client logos"
  And each logo img has alt text with the company name

Scenario: S2-06 — Stats animate on scroll into view
  Given the stats section is below the fold
  When I scroll until it enters the viewport
  Then each stat animates counting from 0 to its final value (Alpine.js + IntersectionObserver)
  And values shown: "10M+", "<250ms", "340%", "50+"

Scenario: S2-07 — Stats responsive grid
  Given viewport < 768px
  Then stats display in a 2x2 grid

Scenario: S2-08 — Capabilities section renders 5 cards
  Given the capabilities section
  Then I see overline "WHAT WE DO", headline "One Platform. Complete Intelligence."
  And 5 FeatureCards linking to their respective /solutions/{slug} pages

Scenario: S2-09 — Capabilities responsive layout
  Given viewport < 768px
  Then feature cards stack in a single column

Scenario: S2-10 — Process flow renders 4 steps
  Given the "How It Works" section
  Then I see: Intake → Enrich & Segment → Score & Route → Deliver
  And "< 250ms end-to-end enrichment" below the flow

Scenario: S2-11 — Process flow responsive
  Given viewport < 768px
  Then the 4 steps stack vertically with downward arrows

Scenario: S2-12 — Industries section renders 3 cards
  Given the industries section
  Then I see Auto Insurance, Health Insurance, Commercial Insurance cards
  And each links to /industries/{slug}

Scenario: S2-13 — Testimonial carousel renders
  Given the testimonial section (navy bg)
  Then I see a quote, person photo, name, title, company
  And carousel dots (5 total) and prev/next arrows

Scenario: S2-14 — Testimonial auto-advances and pauses
  Given the testimonial carousel
  Then it auto-advances every 6 seconds (Alpine.js x-init setInterval)
  And pauses on hover

Scenario: S2-15 — Testimonial swipeable on mobile
  Given viewport < 768px
  When I swipe left on the testimonial
  Then it advances to the next slide

Scenario: S2-16 — Testimonial accessibility
  Given the testimonial carousel
  Then it has role="region" and aria-roledescription="carousel"
  And each slide has role="group" and aria-label

Scenario: S2-17 — Case study preview cards render
  Given the case study section
  Then I see 2 cards with: client logo, key metrics (bold), title, industry, date
  And "Read Case Study" links
  And "View All Case Studies" link below

Scenario: S2-18 — Final CTA section renders
  Given the final CTA section (navy bg)
  Then I see "Ready to Make Every Prospect Count?"
  And buttons: "Request a Demo", "Contact Sales"
  And checkmarks: "No commitment", "30-min overview", "Custom ROI analysis"

Scenario: S2-19 — Homepage meta tags
  Given the homepage
  Then title is "The Switchboard | Insurance Intelligence Platform"
  And meta description mentions risk segmentation and profitable growth
  And OG image, OG title, and Twitter card are set

Scenario: S2-20 — Homepage Organization schema
  Given the homepage source
  Then it includes Organization JSON-LD with name, url, logo, contactPoint

Scenario: S2-21 — All homepage data comes from database
  Given the admin panel has seeded testimonials, logos, case studies, stats
  Then the homepage renders content from PostgreSQL via EF Core
  And changing content in admin updates the homepage

Scenario: S2-22 — Homepage loads in < 3 seconds on 3G
  Given a throttled 3G connection
  When I load the homepage
  Then first meaningful paint occurs within 3 seconds

Scenario: S2-23 — Alpine.js components initialize
  Given the homepage loads
  Then all Alpine.js components (carousel, counter, mega-menu) initialize
  And no JavaScript errors in console

Scenario: S2-24 — Smooth scroll for "See How It Works"
  When I click "See How It Works" CTA
  Then the page smooth-scrolls to the "How It Works" section

Scenario: S2-25 — Social proof logos from database
  Given 6 client logos uploaded via admin
  Then the logo carousel displays all 6
  And images are served via ImageSharp pipeline in WebP format

Scenario: S2-26 — Stats values from database
  Given stats configured in admin (SiteSettings)
  Then the homepage displays current values
  And updating a stat in admin updates the homepage

Scenario: S2-27 — Testimonials from database
  Given 5 testimonials created via admin
  Then the carousel shows all 5
  With photo, quote, name, title, company

Scenario: S2-28 — No "lead" language on homepage
  Given all visible text on the homepage
  When I search for "lead" (case-insensitive, whole word)
  Then zero matches found
```

---

## Slice 3: Core Conversion Pages + Admin Panel Foundation
**Goal:** Ship Contact, Demo, About pages. Bootstrap the admin panel so content is manageable. Wire form submissions to Phoenix CRM.

### What Ships
- **Contact page** — 4-step multi-step form (Alpine.js step wizard) + sidebar
- **Demo page** — Self-built booking widget + "What you'll see" + social proof
- **About page** — Story + values + team grid + compliance badges + metrics
- **Admin panel foundation** — Login, dashboard, content CRUD for testimonials, team members, client logos, site settings
- Contact form API endpoint with FluentValidation + rate limiting
- XSS sanitization on all user inputs (HtmlSanitizer library)
- Phoenix CRM webhook on form submission
- MailKit confirmation emails

### BDD Scenarios (40)

```gherkin
# --- Contact Page ---

Scenario: S3-01 — Contact page renders hero
  Given I navigate to /contact
  Then I see headline "Let's Talk About Your Growth"

Scenario: S3-02 — Multi-step form shows progress (Alpine.js)
  Given the contact form
  Then I see "Step 1 of 4" with progress indicator
  And Alpine.js x-data manages current step state

Scenario: S3-03 — Step 1 collects contact info
  Given Step 1: First Name*, Last Name*, Work Email*, Phone
  And a "Next Step" button

Scenario: S3-04 — Step validation prevents advancement
  Given Step 1 with empty required fields
  When I click "Next Step"
  Then client-side Alpine.js validation shows errors
  And the form does NOT advance

Scenario: S3-05 — Step 2 collects company info
  Given Step 2: Company Name*, Title*, Company Size* (1-10, 11-50, 51-200, 201-500, 500+)

Scenario: S3-06 — Step 3 collects needs assessment
  Given Step 3: Insurance Lines (multi-select), Monthly Volume, Biggest Challenge

Scenario: S3-07 — Step 4 confirmation with TCPA consent
  Given Step 4: Additional Message (optional), TCPA checkbox* with link
  And "Submit" button

Scenario: S3-08 — Form submission requires TCPA consent
  Given Step 4 without TCPA checked
  When I click Submit
  Then error "You must agree to the terms"

Scenario: S3-09 — Form submits via fetch POST
  Given all steps valid and TCPA checked
  When I click Submit
  Then Alpine.js posts JSON to /api/contact
  And success state shows confirmation message

Scenario: S3-10 — Contact API validates with FluentValidation
  Given a POST to /api/contact with invalid email
  Then the API returns 400 with validation errors

Scenario: S3-11 — Contact API sanitizes XSS
  Given message containing "<script>alert('xss')</script>"
  Then HtmlSanitizer strips script tags before processing

Scenario: S3-12 — Contact API rate limits
  Given 10 submissions from same IP in 1 minute
  Then 11th returns 429 Too Many Requests

Scenario: S3-13 — Form submission webhooks to Phoenix CRM
  Given a successful form submission
  Then PhoenixCrmService sends payload to Phoenix API
  With: name, email, phone, company, title, size, lines, volume, challenge

Scenario: S3-14 — Confirmation email sent via MailKit
  Given a successful submission
  Then the submitter receives a confirmation email
  And the internal team receives a notification email

Scenario: S3-15 — Contact sidebar shows alternatives
  Given the contact page
  Then sidebar shows: phone (323) 471-6020, email, address, "Book a Demo" link

Scenario: S3-16 — Contact mobile stacks sidebar below form
  Given viewport < 768px
  Then sidebar stacks below form with click-to-call button prominent

# --- Demo Page ---

Scenario: S3-17 — Demo page renders
  Given /demo
  Then I see "See The Switchboard in Action" and "30-minute personalized walkthrough"

Scenario: S3-18 — Booking widget shows calendar
  Given the demo page
  Then the right column shows a self-built calendar widget (Alpine.js)
  With available dates highlighted and time slots below

Scenario: S3-19 — Booking widget selects date and time
  When I click a date
  Then available time slots appear below
  When I click a time slot
  Then it highlights as selected
  And "Confirm Booking" button enables

Scenario: S3-20 — Booking confirmation
  When I click "Confirm Booking" with name/email filled
  Then appointment is saved to DB
  And confirmation email sent via MailKit
  And success message displayed

Scenario: S3-21 — Demo page shows "What You'll See" checklist
  Given the demo page
  Then left column shows: live enrichment demo, risk segmentation in action,
  ROI projection, Q&A with team

Scenario: S3-22 — Demo social proof bar
  Given the demo page
  Then I see "Join 50+ agencies who started with a demo" with logo row

# --- About Page ---

Scenario: S3-23 — About hero renders
  Given /about
  Then headline "We Built the Intelligence Layer That Insurance Deserves"

Scenario: S3-24 — Our Story 2-column layout
  Given the about page
  Then team photo on left, story text on right
  With "THE PROBLEM WE SAW" and "THE SOLUTION WE BUILT"

Scenario: S3-25 — Company metrics from database
  Given SiteSettings in DB
  Then metrics bar shows: Founded, Prospects Processed, Agency Partners, Latency, Uptime

Scenario: S3-26 — Values grid renders 3 cards
  Given the about page
  Then 3 value cards: Data-Driven, Insurance-First, Relentless Optimization

Scenario: S3-27 — Team grid from database
  Given team members in DB
  Then grid shows headshot, name, title, LinkedIn, bio for each

Scenario: S3-28 — Compliance badges visible
  Given the about page
  Then SOC 2, TCPA, Encrypted, CCPA badges visible with link to /security

# --- Admin Panel Foundation ---

Scenario: S3-29 — Admin login page
  Given /admin
  Then I see a login form
  When I enter valid credentials
  Then I'm redirected to /admin/dashboard

Scenario: S3-30 — Admin requires authentication
  Given I am not logged in
  When I navigate to /admin/dashboard
  Then I am redirected to /admin/login

Scenario: S3-31 — Admin dashboard shows overview
  Given I'm logged into admin
  Then I see: total page views (today/week/month), form submissions count,
  recent submissions list, content counts

Scenario: S3-32 — Admin CRUD for testimonials
  Given I navigate to admin testimonials
  Then I can create, read, update, delete testimonials
  With fields: quote, name, title, company, photo upload, rating

Scenario: S3-33 — Admin CRUD for team members
  Given admin team members page
  Then I can manage: name, title, bio, LinkedIn URL, headshot upload, display order

Scenario: S3-34 — Admin CRUD for client logos
  Given admin client logos page
  Then I can upload logos, set company name, set display order, toggle active

Scenario: S3-35 — Admin site settings
  Given admin settings page
  Then I can edit: stat values (prospects processed, partners, etc.), company info,
  social links, contact details

Scenario: S3-36 — Admin image upload uses ImageSharp
  Given I upload an image via admin
  Then ImageSharp auto-generates: thumbnail, medium, large sizes
  And WebP format versions
  And stores metadata in DB

Scenario: S3-37 — Form submissions viewable in admin
  Given contact form submissions in DB
  Then admin shows a paginated list with: name, email, company, date, status
  And clicking a row shows full submission details

Scenario: S3-38 — Meta tags on all pages
  Given /contact, /demo, /about
  Then each has unique title and meta description

Scenario: S3-39 — Breadcrumbs on all pages
  Given /contact, /demo, /about
  Then breadcrumbs show "Home > [Page Name]"

Scenario: S3-40 — No "lead" language
  Given all visible text on /contact, /demo, /about, admin
  Then zero instances of "lead" in customer-facing text
```

---

## Slice 4: Solutions Pages
**Goal:** Product depth. Overview + 5 individual solution pages. Admin panel for managing solution content.

### What Ships
- Solutions overview (`/solutions`) — interactive platform diagram + alternating cards + comparison table
- 5 individual solution pages via dynamic route (`/solutions/{slug}`)
- Interactive platform diagram (Alpine.js click handlers)
- Comparison table component
- FAQ accordion component (Alpine.js x-collapse) with Schema.org JSON-LD
- Admin: solution page content management (hero text, features, FAQ entries)
- Reusable solution page Razor template

### BDD Scenarios (28)

```gherkin
Scenario: S4-01 — Solutions overview hero with breadcrumb
  Given /solutions
  Then breadcrumb "Home > Solutions", overline "OUR PLATFORM"
  And headline "Complete Insurance Intelligence, One Integrated Platform"

Scenario: S4-02 — Interactive platform diagram renders
  Given the solutions overview
  Then flow diagram: Sources → The Switchboard → Delivery + Feedback Loop
  And each node is clickable (Alpine.js @click navigates to solution page)

Scenario: S4-03 — Platform diagram keyboard accessible
  Given the diagram nodes
  Then each has tabindex and aria-label
  And Enter/Space navigates to the solution page

Scenario: S4-04 — Alternating solution cards (5)
  Given the overview page
  Then 5 cards alternate image-left/right
  With number, title, description, checkmarks, "Learn More" link

Scenario: S4-05 — Comparison table renders
  Given the overview page
  Then "Why The Switchboard?" table with columns: Feature, The Switchboard, Typical Vendor, DIY

Scenario: S4-06 — Comparison table scrolls horizontally on mobile
  Given viewport < 768px
  Then table scrolls horizontally with Feature column sticky

Scenario: S4-07 — Solution page hero (e.g., Risk Segmentation)
  Given /solutions/risk-segmentation
  Then breadcrumb, overline, headline, product visual, "Request a Demo" CTA

Scenario: S4-08 — Key metrics bar (4 metrics)
  Given a solution page
  Then 4 metric cards with large numbers and labels from DB

Scenario: S4-09 — Problem section with 3 pain point cards
  Given a solution page
  Then "THE CHALLENGE" with 3 problem cards (X icon, title, description)

Scenario: S4-10 — 3-step solution process
  Given a solution page
  Then "HOW IT WORKS" with 3 connected steps and descriptions

Scenario: S4-11 — Features detail blocks (alternating)
  Given a solution page
  Then "CAPABILITIES" with alternating screenshot/text blocks and checkmarks

Scenario: S4-12 — Related case study on navy bg
  Given a solution page
  Then featured case study with logo, quote, metrics, "Read Full Case Study" link

Scenario: S4-13 — FAQ accordion works (Alpine.js)
  Given a solution page FAQ section
  Then clicking a question opens the answer (x-collapse transition)
  And opening one closes the previously open one

Scenario: S4-14 — FAQ Schema.org markup
  Given the page source
  Then FAQPage JSON-LD exists with all Q&A pairs

Scenario: S4-15 — Data Enrichment page content
  Given /solutions/data-enrichment
  Then mentions: 90GB+ Equifax data, demographic, financial, behavioral, property/vehicle

Scenario: S4-16 — Risk Segmentation page content
  Given /solutions/risk-segmentation
  Then mentions: multi-factor scoring, carrier matching, A-F categories, custom models

Scenario: S4-17 — Quality Scoring page content
  Given /solutions/quality-scoring
  Then mentions: ML algorithms, conversion prediction, <250ms, prospect grading

Scenario: S4-18 — Performance Analytics page content
  Given /solutions/analytics
  Then mentions: closed-loop ROI, real-time dashboards, acquisition metrics

Scenario: S4-19 — Decision Engine page content
  Given /solutions/decision-engine
  Then mentions: real-time routing, buyer matching, price optimization, API delivery

Scenario: S4-20 — Dynamic routing resolves all 5 slugs
  Given slugs: data-enrichment, risk-segmentation, quality-scoring, analytics, decision-engine
  Then each returns 200. Invalid slug returns 404.

Scenario: S4-21 — Solution content from database
  Given solution data in DB
  Then pages render from DB content
  And admin can edit hero text, features, FAQ entries

Scenario: S4-22 — Admin CRUD for solutions
  Given admin solutions page
  Then I can manage: slug, title, overline, hero text, metrics, features, FAQs

Scenario: S4-23 — Each solution has unique meta tags
  Given any solution page
  Then title includes solution name, meta description is unique

Scenario: S4-24 — Solution pages include final CTA
  Given any solution page
  Then CTA section appears before footer

Scenario: S4-25 — Related solutions links
  Given /solutions/risk-segmentation
  Then links to related solutions appear (Data Enrichment, Quality Scoring)

Scenario: S4-26 — Solution pages pass CWV
  Given any solution page
  Then LCP < 2.5s, INP < 200ms, CLS < 0.1

Scenario: S4-27 — Product schema markup
  Given /solutions page source
  Then Schema.org Product JSON-LD exists

Scenario: S4-28 — No "lead" language
  Given all solution page text
  Then zero instances of "lead" in customer-facing content
```

---

## Slice 5: Industry Pages
**Goal:** Vertical-specific depth. Auto, Health, Commercial. Show buyers you know their world.

### What Ships
- Reusable industry page Razor template (`/industries/{slug}`)
- Auto Insurance, Health Insurance, Commercial Insurance pages
- Industry-specific stats, enrichment data grids, process flows
- Cross-links to other industries + relevant solutions
- Industry case study highlights
- Admin: industry page content management

### BDD Scenarios (20)

```gherkin
Scenario: S5-01 — Auto Insurance hero
  Given /industries/auto
  Then overline "AUTO INSURANCE INTELLIGENCE"
  And headline "Risk-Scored Auto Prospects That Your Underwriters Will Love"
  And CTAs: "Request a Demo", "See Auto Case Study"

Scenario: S5-02 — Industry stats from DB
  Given /industries/auto
  Then stats show: 2.1M+ scored, 38% better loss ratio, <200ms enrichment, 15+ carriers

Scenario: S5-03 — Enrichment data grid (Auto)
  Given /industries/auto
  Then 3 cards: Vehicle Data (make/model/year/VIN/ZIP), Driver History (MVR/claims/gaps), Risk Profile (tier/LTV/conversion/price)

Scenario: S5-04 — Industry process flow
  Given an industry page
  Then 4-step process customized for that vertical

Scenario: S5-05 — Industry case study highlight
  Given /industries/auto
  Then featured case study with auto-specific metrics

Scenario: S5-06 — Cross-industry links
  Given /industries/auto
  Then links to Health, Commercial, Life

Scenario: S5-07 — Health Insurance content
  Given /industries/health
  Then mentions ACA, Medicare, compliance, enrollment periods

Scenario: S5-08 — Commercial Insurance content
  Given /industries/commercial
  Then mentions business class codes, risk profiling, commercial lines

Scenario: S5-09 — Industry FAQ with schema
  Given any industry page
  Then FAQ accordion with industry questions + FAQPage JSON-LD

Scenario: S5-10 — Dynamic routing (3 slugs)
  Given slugs: auto, health, commercial
  Then each returns 200. Invalid slug returns 404.

Scenario: S5-11 — Industry content from DB
  Given industry data in DB
  Then admin can edit all industry page content

Scenario: S5-12 — Admin CRUD for industries
  Given admin industries page
  Then manage: slug, title, stats, enrichment data, FAQ entries, case study link

Scenario: S5-13 — Unique meta tags per industry
  Given /industries/auto
  Then title "Auto Insurance Intelligence | The Switchboard"

Scenario: S5-14 — Breadcrumbs
  Given /industries/health
  Then "Home > Industries > Health Insurance"

Scenario: S5-15 — Cross-links to solutions
  Given any industry page
  Then links to relevant solution pages

Scenario: S5-16 — Industry images via ImageSharp
  Given industry page images
  Then served in WebP with responsive srcset

Scenario: S5-17 — Industry responsive layout
  Given viewport < 768px
  Then enrichment cards + cross-links stack vertically

Scenario: S5-18 — Final CTA on industry pages
  Given any industry page
  Then CTA section before footer

Scenario: S5-19 — Industry pages pass CWV
  Given any industry page
  Then LCP < 2.5s, INP < 200ms, CLS < 0.1

Scenario: S5-20 — No "lead" language
  Given all industry page text
  Then zero instances
```

---

## Slice 6: Content Engine (Blog + Case Studies + Resources Hub)
**Goal:** Build the content marketing machine. All self-built — no Sanity, no WordPress. TipTap rich text editor in admin.

### What Ships
- Admin: Blog post editor (TipTap rich text, image upload, categories, tags, SEO fields, scheduling)
- Admin: Case study editor (structured fields: company, challenge, solution, results, metrics)
- Admin: Author profiles, FAQ management
- Resources hub (`/resources`) with filtering + search
- Blog listing (`/resources/blog`) + post template (`/resources/blog/{slug}`)
- Case study listing (`/resources/case-studies`) + template (`/resources/case-studies/{slug}`)
- Blog sidebar: TOC (sticky), CTA, related posts, newsletter
- Case study sidebar: quick facts, testimonial quote, CTA
- Gated content system (email gate for whitepapers, progressive profiling)
- RSS feed (`/resources/blog/rss`)
- Content scheduling background service
- Related content engine (tag-based matching)
- Pagination component

### BDD Scenarios (30)

```gherkin
Scenario: S6-01 — Resources hub with filters
  Given /resources
  Then hero "Insights & Intelligence"
  And tabs: All, Blog, Case Studies, Whitepapers, Webinars
  And dropdowns: Industry, Topic
  And search bar

Scenario: S6-02 — Filter by type
  When I click "Case Studies" tab
  Then only case studies show. URL updates with query param.

Scenario: S6-03 — Featured resource at top
  Given an item flagged as featured in admin
  Then it renders full-width at the top

Scenario: S6-04 — Resource grid 3-column with pagination
  Given 12+ resources
  Then 3-column grid (9 per page) with pagination

Scenario: S6-05 — Blog listing renders
  Given /resources/blog
  Then grid of post cards: image, category, title, excerpt, date, read time

Scenario: S6-06 — Blog post full layout
  Given /resources/blog/{slug}
  Then: breadcrumb, category, title, author (photo/name/title), date, hero image,
  rich text body (from TipTap), share buttons, tags, author bio

Scenario: S6-07 — Blog sidebar (desktop)
  Given a blog post on desktop
  Then sidebar: sticky TOC, CTA box, Related Posts, Newsletter signup

Scenario: S6-08 — Blog TOC tracks scroll position
  Given blog post with H2 headings
  Then TOC highlights current section on scroll (Alpine.js + IntersectionObserver)

Scenario: S6-09 — Blog Article schema
  Given blog post source
  Then Article + BlogPosting JSON-LD with author, date, headline, image

Scenario: S6-10 — Case study listing
  Given /resources/case-studies
  Then cards with: client logo, title, key metric, industry

Scenario: S6-11 — Case study full layout
  Given /resources/case-studies/{slug}
  Then: hero with logo + title, key results bar (4 metrics),
  narrative (Company, Challenge, Solution, Results), before/after table

Scenario: S6-12 — Case study sidebar
  Given a case study
  Then sidebar: Quick Facts (industry, size, volume, products, timeline),
  testimonial quote, "Get Similar Results" CTA

Scenario: S6-13 — Gated content email gate
  Given a whitepaper
  When I click "Download"
  Then email capture modal appears (Alpine.js)
  And download only proceeds after email submitted

Scenario: S6-14 — Progressive profiling
  Given I've already downloaded one resource (cookie set)
  Then next gate asks for company + title (not just email)

Scenario: S6-15 — Newsletter signup
  Given newsletter form
  When I submit valid email
  Then subscriber saved to DB, success message shown

Scenario: S6-16 — Related content by tags
  Given a blog post tagged ["risk-segmentation", "auto"]
  Then related posts section shows matching tagged posts

Scenario: S6-17 — Author profile component
  Given a blog post with author
  Then author bio: photo, name, title, bio, LinkedIn

Scenario: S6-18 — Admin blog editor with TipTap
  Given admin blog editor
  Then TipTap rich text with: headings, bold, italic, lists, images, blockquotes, code
  And slug auto-generated from title
  And SEO fields: meta title, meta description, OG image
  And category + tag selection
  And "Publish now" or "Schedule for" datetime picker

Scenario: S6-19 — Admin case study editor
  Given admin case study editor
  Then structured fields: client name, logo, industry, challenge, solution, results,
  before/after metrics, testimonial quote, quick facts

Scenario: S6-20 — Content scheduling works
  Given a blog post scheduled for future datetime
  Then ContentSchedulerService publishes it at that time
  And it appears on the blog listing

Scenario: S6-21 — Draft/Published workflow
  Given a blog post in "Draft" status
  Then it's visible in admin but NOT on the public site
  When I change to "Published"
  Then it appears on the blog

Scenario: S6-22 — RSS feed
  Given /resources/blog/rss
  Then valid RSS XML with recent blog posts

Scenario: S6-23 — Blog search
  Given the resources search bar
  When I type "risk segmentation"
  Then results filter to matching resources (server-side query)

Scenario: S6-24 — Resources responsive
  Given viewport < 768px
  Then cards stack single column, filter bar scrolls horizontally

Scenario: S6-25 — Blog sidebar collapses on mobile
  Given viewport < 1024px
  Then sidebar moves below article, TOC becomes collapsible

Scenario: S6-26 — Admin author management
  Given admin authors page
  Then CRUD: name, title, bio, photo, LinkedIn, Twitter

Scenario: S6-27 — Image uploads via TipTap
  Given TipTap editor
  When I upload an image
  Then ImageSharp processes it (resize, WebP) and inserts into content

Scenario: S6-28 — Content meta tags from DB
  Given a blog post with SEO fields filled
  Then page title and meta description come from those fields

Scenario: S6-29 — Pagination component
  Given 30 blog posts at 9 per page
  Then pagination shows: Previous, 1, 2, 3, ..., Next

Scenario: S6-30 — No "lead" language in templates
  Given all template/component text (not user-authored content)
  Then zero instances of "lead"
```

---

## Slice 7: Self-Built Tools (Analytics, Chat, SEO, A/B Testing)
**Goal:** Replace every 3rd-party tool. Own your analytics, chat, SEO toolkit, and A/B testing.

### What Ships
- **First-party analytics engine** — AnalyticsMiddleware + lightweight client JS. Page views, events, scroll depth, click heatmaps. Admin dashboard.
- **Chat widget** — SignalR hub. Business hours = live chat. Off hours = AI chatbot (Claude API). Transcripts in DB. Admin view.
- **SEO toolkit** — Admin: meta editor per page, redirect manager (301s from DB), sitemap.xml generator, Schema.org builder, broken link checker
- **A/B testing framework** — ABTestMiddleware assigns variants. Track conversions. Admin shows results with significance.
- **ROI calculator** — Industry-specific models. Configurable assumptions in admin. Results gated behind email. PDF export.
- **Uptime monitor** — Background service, status page at `/status`, email alerts
- **OG image generator** — ImageSharp renders branded images per page
- **Performance dashboard** — Client JS reports CWV. Admin shows per-route metrics.
- **Security page** (`/security`)
- **Exit-intent modal** (Alpine.js, desktop only, once per session)

### BDD Scenarios (32)

```gherkin
# --- Analytics Engine ---

Scenario: S7-01 — Page view tracked server-side
  Given any page load
  Then AnalyticsMiddleware records: path, referrer, user-agent, timestamp, session ID

Scenario: S7-02 — Client events tracked via JS
  Given lightweight analytics.js loaded
  Then it tracks: scroll depth (25/50/75/100%), CTA clicks, form interactions
  And posts events to /api/analytics/event

Scenario: S7-03 — Click heatmap data collected
  Given analytics.js
  Then click coordinates recorded and posted to /api/analytics/clicks

Scenario: S7-04 — Analytics admin dashboard
  Given admin analytics
  Then I see: page views (today/week/month), top pages, referrer sources,
  device breakdown, session duration averages

Scenario: S7-05 — Heatmap visualization in admin
  Given admin heatmap view
  Then I see click density overlay on page screenshots

Scenario: S7-06 — No cookie consent needed
  Given first-party analytics with no PII
  Then no cookie consent banner is required for analytics
  And no data sent to third parties

# --- Chat Widget ---

Scenario: S7-07 — Chat bubble renders on all pages
  Given any page
  Then a chat bubble icon in bottom-right (Alpine.js)

Scenario: S7-08 — Chat opens on click
  When I click the chat bubble
  Then SignalR chat window opens with greeting

Scenario: S7-09 — Live chat during business hours
  Given current time is within business hours
  Then messages route to the live chat admin panel
  And admin can respond in real-time

Scenario: S7-10 — AI chatbot off hours
  Given current time is outside business hours
  Then messages are answered by Claude API
  With FAQ-trained responses about The Switchboard

Scenario: S7-11 — Chat transcripts saved
  Given a chat conversation
  Then all messages stored in DB
  And visible in admin chat panel with session info

Scenario: S7-12 — Chat widget accessibility
  Given the chat bubble
  Then aria-label "Open chat", keyboard navigable, focus trapped when open

# --- SEO Toolkit ---

Scenario: S7-13 — Admin meta editor
  Given admin SEO page
  Then I can edit title + meta description for every page/route
  And preview how it looks in Google search results

Scenario: S7-14 — Redirect manager
  Given admin redirects page
  Then I can add 301 redirects: source path → destination path
  And RedirectMiddleware serves them server-side

Scenario: S7-15 — Sitemap.xml auto-generated
  Given /sitemap.xml
  Then valid XML with all public pages from DB
  With lastmod, changefreq, priority per URL

Scenario: S7-16 — Robots.txt configured
  Given /robots.txt
  Then allows all crawlers, references sitemap, blocks /admin and /api

Scenario: S7-17 — Schema.org builder in admin
  Given admin schema builder
  Then I can configure JSON-LD per page type
  And it auto-injects into page source

Scenario: S7-18 — Broken link checker
  Given admin SEO tools
  When I run broken link check
  Then it crawls all internal links and reports 404s

# --- A/B Testing ---

Scenario: S7-19 — A/B test variant assignment
  Given an active experiment for the homepage hero
  Then ABTestMiddleware assigns visitor to variant A or B (cookie-based)
  And the page renders the assigned variant

Scenario: S7-20 — A/B test conversion tracking
  Given a visitor in variant B clicks "Request a Demo"
  Then conversion is recorded for variant B

Scenario: S7-21 — A/B test admin results
  Given admin A/B test page
  Then I see: experiment name, variants, visitor counts, conversion rates,
  statistical significance indicator

# --- ROI Calculator ---

Scenario: S7-22 — ROI calculator widget renders
  Given the ROI calculator
  Then inputs: current monthly volume, current CPA, current close rate

Scenario: S7-23 — ROI calculator computes
  Given inputs filled
  When I click "Calculate My ROI"
  Then projected improvements shown (CPA reduction, close rate increase, annual savings)

Scenario: S7-24 — ROI results gated
  Given calculated results
  Then partial results visible, full results require email

# --- Uptime Monitor ---

Scenario: S7-25 — Status page renders
  Given /status
  Then shows: current status (operational/degraded/down), uptime percentage,
  response time graph, incident history

Scenario: S7-26 — Uptime monitor checks endpoints
  Given UptimeMonitorService running
  Then it pings /health every 60 seconds
  And stores response times in DB

Scenario: S7-27 — Downtime alerts
  Given 3 consecutive failed health checks
  Then email alert sent to team

# --- Other ---

Scenario: S7-28 — Security page renders
  Given /security
  Then compliance badges, encryption details, infrastructure, access control
  And CTAs: "Download Security Whitepaper", "Request SOC 2 Report"

Scenario: S7-29 — Exit-intent modal (desktop only)
  Given desktop viewport and > 10 seconds on page
  When cursor moves above viewport
  Then modal appears with email capture (Alpine.js, once per session cookie)

Scenario: S7-30 — OG images auto-generated
  Given a blog post "5 Risk Strategies"
  Then /og/resources/blog/5-risk-strategies returns a 1200x630 PNG
  With title + author + branding rendered via ImageSharp

Scenario: S7-31 — Performance dashboard in admin
  Given client JS reporting CWV via /api/analytics/vitals
  Then admin shows: LCP, INP, CLS averages per route
  With pass/fail indicators against thresholds

Scenario: S7-32 — No "lead" language
  Given all slice 7 component text
  Then zero instances
```

---

## Slice 8: Polish, Performance & Launch
**Goal:** Production readiness. Performance, accessibility, redirects, cross-browser, llms.txt, final QA.

### What Ships
- Core Web Vitals optimization pass (all pages)
- WCAG 2.1 AA accessibility audit + fixes
- 301 redirects from old URLs (via redirect manager built in Slice 7)
- Cross-browser testing
- Canonical URLs on every page
- llms.txt + AI search optimization
- Privacy Policy page (`/privacy`)
- Terms of Service page (`/terms`)
- Error pages (404, 500)
- Final content review (zero "lead" language)
- Production Railway deployment + custom domain + SSL

### BDD Scenarios (22)

```gherkin
Scenario: S8-01 — All pages pass Core Web Vitals
  Given every page
  Then LCP < 2.5s, INP < 200ms, CLS < 0.1

Scenario: S8-02 — Lighthouse scores > 90
  Given every page
  Then Performance > 90, Accessibility > 90, Best Practices > 90, SEO > 90

Scenario: S8-03 — Images optimized
  Given every image
  Then served via ImageSharp in WebP/AVIF, responsive srcset, lazy load below fold

Scenario: S8-04 — Fonts optimized
  Given Inter font
  Then self-hosted woff2, font-display: swap, only used weights loaded

Scenario: S8-05 — WCAG 2.1 AA compliance
  Given every page
  Then keyboard accessible, contrast AA compliant, alt text on images,
  visible focus indicators, ARIA landmarks (main, nav, footer)

Scenario: S8-06 — Skip to content link
  Given any page, first Tab press
  Then "Skip to main content" link visible and functional

Scenario: S8-07 — 301 redirects from old site
  Given /privacy-policy → 301 to /privacy
  And /terms-of-service → 301 to /terms

Scenario: S8-08 — Sitemap.xml complete
  Given /sitemap.xml
  Then all public pages included with lastmod

Scenario: S8-09 — Robots.txt correct
  Given /robots.txt
  Then allows crawlers, blocks /admin + /api, references sitemap

Scenario: S8-10 — Canonical URLs on every page
  Given any page
  Then <link rel="canonical"> present and correct

Scenario: S8-11 — OG images on all pages
  Given any page
  Then og:image present (auto-generated or custom)

Scenario: S8-12 — llms.txt for AI search
  Given /llms.txt
  Then structured description of site, services, and capabilities
  For AI search engines (Perplexity, SearchGPT, etc.)

Scenario: S8-13 — Cross-browser: Chrome, Safari, Firefox, Edge
  Given the site in each browser
  Then all pages render correctly

Scenario: S8-14 — Cross-browser: iOS Safari + Android Chrome
  Given mobile browsers
  Then responsive, touch interactions work

Scenario: S8-15 — No broken links
  Given every internal link
  Then all return 200 or valid redirect

Scenario: S8-16 — 404 page styled
  Given /nonexistent-page
  Then styled 404 with nav back to homepage

Scenario: S8-17 — 500 error page
  Given a server error
  Then styled 500 page with support contact

Scenario: S8-18 — Privacy policy renders
  Given /privacy
  Then CCPA/GDPR compliant policy with cookie section

Scenario: S8-19 — Terms of service renders
  Given /terms
  Then legal terms page

Scenario: S8-20 — Final "lead" language audit
  Given entire site
  Then zero instances of "lead" in customer-facing text

Scenario: S8-21 — Production deployment
  Given main branch merged
  Then Railway deploys, custom domain resolves, SSL valid, HSTS set

Scenario: S8-22 — /api/search endpoint
  Given /api/search?q=risk+segmentation
  Then returns matching content (blog posts, solutions, case studies)
  For programmatic access and potential AI search indexing
```

---

## Summary

| Slice | Scenarios | Description |
|---|---|---|
| **Slice 1** | 34 | Foundation & Design System (ASP.NET Core 9 + Tailwind + Alpine.js) |
| **Slice 2** | 28 | Homepage (Full Build — all sections, DB-driven) |
| **Slice 3** | 40 | Core Conversion Pages + Admin Panel Foundation |
| **Slice 4** | 28 | Solutions Pages (Overview + 5 Individual) |
| **Slice 5** | 20 | Industry Pages (Auto, Health, Commercial) |
| **Slice 6** | 30 | Content Engine (Blog + Case Studies + Resources Hub + TipTap Admin) |
| **Slice 7** | 32 | Self-Built Tools (Analytics, Chat, SEO, A/B, ROI Calc, Uptime) |
| **Slice 8** | 22 | Polish, Performance & Launch |
| **TOTAL** | **234** | |

---

## Slice Dependencies

```
Slice 1 (Foundation)
  ├── Slice 2 (Homepage) — needs components + DB
  ├── Slice 3 (Conversion + Admin) — needs components + forms + DB
  │     ├── Slice 4 (Solutions) — needs admin + templates
  │     │     └── Slice 5 (Industries) — reuses solution patterns
  │     ├── Slice 6 (Content Engine) — needs admin + TipTap
  │     └── Slice 7 (Tools) — needs admin + middleware
  └── Slice 8 (Polish/Launch) — needs everything built
```

**Critical path:** 1 → 2 → 3 → 4 → 5 → 8
**Parallel tracks after Slice 3:** Slices 4+5, Slice 6, and Slice 7 can run simultaneously.

---

## External Dependencies (minimal)

| Service | Purpose | Cost |
|---|---|---|
| **Railway** | Hosting (Docker + PostgreSQL + Redis) | ~$20-50/mo |
| **Amazon SES** | SMTP relay for email deliverability | ~$1/10K emails |
| **Namecheap** | Domain registrar + DNS | Already owned |
| **Claude API** | AI chatbot for off-hours chat | ~$5-20/mo usage-based |
| **Google Search Console** | SEO indexing monitoring | Free |

Everything else is self-built and self-owned.

---

*234 BDD scenarios across 8 slices. Zero "lead" language. Zero 3rd-party SaaS dependencies.*
*C# + ASP.NET Core 9 + PostgreSQL + Railway. Form submissions → Phoenix CRM.*
*Generated via Memento IDEA Protocol.*
