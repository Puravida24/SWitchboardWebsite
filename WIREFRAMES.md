# The Switchboard — Page Wireframes
**Date:** 2026-03-10
**Status:** Planning — Review & Approve Before Build

---

## Table of Contents
1. [Homepage](#1-homepage)
2. [Solutions Overview](#2-solutions-overview)
3. [Individual Solution Page](#3-individual-solution-page)
4. [Industry Page](#4-industry-page)
5. [About Page](#5-about-page)
6. [Contact Page](#6-contact-page)
7. [Demo Request Page](#7-demo-request-page)
8. [Resources Hub](#8-resources-hub)
9. [Blog Post](#9-blog-post)
10. [Case Study](#10-case-study)
11. [Security Page](#11-security-page)
12. [Header / Navigation](#12-header--navigation)
13. [Footer](#13-footer)
14. [Mobile Variants](#14-mobile-variants)

---

## 12. Header / Navigation

### Desktop (≥1024px)
```
┌──────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│  [LOGO]          Solutions ▾   Industries ▾   Resources ▾   About   Contact  │
│  The Switchboard                                                  [ Request  │
│                                                                     Demo  ]  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                         MEGA MENU (on hover/click)                           │
│                                                                              │
│  Solutions ▾ expands to:                                                     │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  PLATFORM CAPABILITIES              FEATURED RESOURCE                  │  │
│  │                                                                        │  │
│  │  [icon] Data Enrichment             ┌─────────────────────┐           │  │
│  │  Enhance prospect data with         │                     │           │  │
│  │  proprietary intelligence           │  [Case Study Image] │           │  │
│  │                                     │                     │           │  │
│  │  [icon] Risk Segmentation           │  "How Agency X      │           │  │
│  │  Categorize by risk profile         │   increased ROI     │           │  │
│  │  for precision targeting            │   by 340%"          │           │  │
│  │                                     │                     │           │  │
│  │  [icon] Quality Scoring             │  [Read Case Study →]│           │  │
│  │  ML-powered conversion              └─────────────────────┘           │  │
│  │  prediction                                                            │  │
│  │                                                                        │  │
│  │  [icon] Performance Analytics                                          │  │
│  │  Real-time ROI dashboards                                              │  │
│  │                                                                        │  │
│  │  [icon] Decision Engine                                                │  │
│  │  Intelligent routing & pricing                                         │  │
│  │                                                                        │  │
│  │  ─────────────────────────                                             │  │
│  │  [View All Solutions →]                                                │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  Industries ▾ expands to:                                                    │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  [icon] Auto Insurance    [icon] Health Insurance                      │  │
│  │  [icon] Commercial        [icon] Life Insurance                        │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  Resources ▾ expands to:                                                     │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  LEARN                    LATEST FROM BLOG                             │  │
│  │                                                                        │  │
│  │  [icon] Blog              [img] "5 Risk Segmentation                   │  │
│  │  [icon] Case Studies             Strategies for 2026"                  │  │
│  │  [icon] Whitepapers       Mar 5, 2026 · 8 min read                    │  │
│  │  [icon] Webinars                                                       │  │
│  │                           [img] "The True Cost of                      │  │
│  │                                  Unscored Leads"                       │  │
│  │                           Feb 28, 2026 · 6 min read                   │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 1. Homepage

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [HEADER / NAV - see above]                                                   │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                        ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░                        │
│                        ░░░  HERO SECTION (navy bg)  ░░░                      │
│                        ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░                        │
│                                                                              │
│  OVERLINE: INSURANCE INTELLIGENCE PLATFORM                                   │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │         The Intelligence Layer Behind                                  │  │
│  │         Profitable Insurance Growth                                    │  │
│  │                                                      ┌──────────────┐ │  │
│  │  Transform raw prospect data into risk-scored,       │              │ │  │
│  │  conversion-predicted opportunities — delivered      │  [HERO       │ │  │
│  │  in real-time to the right buyer at the              │   IMAGE:     │ │  │
│  │  right price.                                        │   Dashboard  │ │  │
│  │                                                      │   mockup or  │ │  │
│  │  [ Request a Demo ]  [ See How It Works ]            │   abstract   │ │  │
│  │   (primary btn)       (ghost btn)                    │   data viz]  │ │  │
│  │                                                      │              │ │  │
│  │  "Trusted by 50+ insurance agencies                  └──────────────┘ │  │
│  │   processing 10,000+ leads daily"                                      │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                    ░░░ SOCIAL PROOF BAR (light bg) ░░░                       │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  "Trusted by leading insurance organizations"                          │  │
│  │                                                                        │  │
│  │  [Logo 1]  [Logo 2]  [Logo 3]  [Logo 4]  [Logo 5]  [Logo 6]         │  │
│  │                                                                        │  │
│  │  ← auto-scrolling carousel with 12+ logos →                           │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                    ░░░ STATS COUNTER SECTION ░░░                             │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐              │  │
│  │  │          │  │          │  │          │  │          │              │  │
│  │  │  10M+    │  │  <250    │  │  340%    │  │  50+     │              │  │
│  │  │  Leads   │  │  ms      │  │  Avg ROI │  │  Agency  │              │  │
│  │  │ Processed│  │ Enrichment│  │  Lift    │  │ Partners │              │  │
│  │  │          │  │  Time    │  │          │  │          │              │  │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘              │  │
│  │                                                                        │  │
│  │  (animated counting on scroll into view)                               │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                ░░░ PLATFORM CAPABILITIES (white bg) ░░░                      │
│                                                                              │
│  OVERLINE: WHAT WE DO                                                        │
│  "One Platform. Complete Intelligence."                                      │
│  Subtitle text describing the platform                                       │
│                                                                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐          │
│  │                  │  │                  │  │                  │          │
│  │  [icon]          │  │  [icon]          │  │  [icon]          │          │
│  │                  │  │                  │  │                  │          │
│  │  Data            │  │  Risk            │  │  Quality         │          │
│  │  Enrichment      │  │  Segmentation    │  │  Scoring         │          │
│  │                  │  │                  │  │                  │          │
│  │  Enhance prospect│  │  Categorize by   │  │  ML algorithms   │          │
│  │  data with 90GB+ │  │  risk profiles   │  │  predict         │          │
│  │  of proprietary  │  │  for precision   │  │  conversion      │          │
│  │  intelligence    │  │  targeting &     │  │  likelihood for  │          │
│  │  from Equifax    │  │  optimal pricing │  │  every prospect  │          │
│  │  and carrier     │  │  strategies.     │  │  in <250ms.      │          │
│  │  data sources.   │  │                  │  │                  │          │
│  │                  │  │                  │  │                  │          │
│  │  [Learn More →]  │  │  [Learn More →]  │  │  [Learn More →]  │          │
│  │                  │  │                  │  │                  │          │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘          │
│                                                                              │
│  ┌──────────────────┐  ┌──────────────────┐                                 │
│  │                  │  │                  │                                 │
│  │  [icon]          │  │  [icon]          │                                 │
│  │                  │  │                  │                                 │
│  │  Performance     │  │  Decision        │                                 │
│  │  Analytics       │  │  Engine          │                                 │
│  │                  │  │                  │                                 │
│  │  Closed-loop ROI │  │  Real-time       │                                 │
│  │  tracking with   │  │  routing, buyer  │                                 │
│  │  real-time       │  │  matching &      │                                 │
│  │  dashboards.     │  │  price           │                                 │
│  │                  │  │  optimization.   │                                 │
│  │  [Learn More →]  │  │  [Learn More →]  │                                 │
│  │                  │  │                  │                                 │
│  └──────────────────┘  └──────────────────┘                                 │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│              ░░░ HOW IT WORKS — PLATFORM FLOW (gray bg) ░░░                 │
│                                                                              │
│  OVERLINE: HOW IT WORKS                                                      │
│  "From Raw Data to Revenue in Milliseconds"                                  │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │   STEP 1              STEP 2              STEP 3             STEP 4   │  │
│  │                                                                        │  │
│  │  ┌────────┐       ┌────────────┐       ┌──────────┐      ┌────────┐  │  │
│  │  │        │       │            │       │          │      │        │  │  │
│  │  │  Lead  │ ───→  │  Enrich &  │ ───→  │  Score & │ ──→  │Deliver │  │  │
│  │  │ Intake │       │  Segment   │       │  Route   │      │        │  │  │
│  │  │        │       │            │       │          │      │        │  │  │
│  │  └────────┘       └────────────┘       └──────────┘      └────────┘  │  │
│  │                                                                        │  │
│  │  Prospects enter   90GB+ Equifax       ML models score    Risk-scored  │  │
│  │  via quote forms,  data merged.        conversion         leads        │  │
│  │  partner APIs,     Risk profiles       likelihood.        delivered    │  │
│  │  and networks.     assigned in         Best buyer match   via API or   │  │
│  │                    real-time.          calculated.         feed.        │  │
│  │                                                                        │  │
│  │                    < 250ms end-to-end enrichment >                     │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                ░░░ INDUSTRIES SERVED (white bg) ░░░                          │
│                                                                              │
│  OVERLINE: INDUSTRIES                                                        │
│  "Purpose-Built for Insurance"                                               │
│                                                                              │
│  ┌─────────────────────┐ ┌─────────────────────┐ ┌─────────────────────┐   │
│  │                     │ │                     │ │                     │   │
│  │  [Auto imagery]     │ │  [Health imagery]   │ │  [Commercial img]   │   │
│  │                     │ │                     │ │                     │   │
│  │  AUTO INSURANCE     │ │  HEALTH INSURANCE   │ │  COMMERCIAL         │   │
│  │                     │ │                     │ │  INSURANCE           │   │
│  │  Risk-scored auto   │ │  ACA & Medicare     │ │  Business class     │   │
│  │  leads with driver  │ │  lead intelligence  │ │  code matching &    │   │
│  │  history, vehicle   │ │  with compliance    │ │  risk profiling     │   │
│  │  data, and carrier  │ │  built in.          │ │  for every line.    │   │
│  │  matching.          │ │                     │ │                     │   │
│  │                     │ │                     │ │                     │   │
│  │  [Explore →]        │ │  [Explore →]        │ │  [Explore →]        │   │
│  │                     │ │                     │ │                     │   │
│  └─────────────────────┘ └─────────────────────┘ └─────────────────────┘   │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│               ░░░ TESTIMONIAL SECTION (navy bg) ░░░                         │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │                          ★ ★ ★ ★ ★                                    │  │
│  │                                                                        │  │
│  │  "The Switchboard transformed our customer acquisition.                │  │
│  │   We went from buying blind leads to receiving                         │  │
│  │   risk-scored, conversion-predicted opportunities.                     │  │
│  │   Our close rate increased 47% in the first quarter."                  │  │
│  │                                                                        │  │
│  │                     ┌──────┐                                           │  │
│  │                     │[photo│                                           │  │
│  │                     │  ]   │                                           │  │
│  │                     └──────┘                                           │  │
│  │                   Sarah Mitchell                                       │  │
│  │              VP of Growth, Apex Insurance Group                        │  │
│  │                                                                        │  │
│  │                    ● ○ ○ ○ ○                                          │  │
│  │               (carousel dots - 5 testimonials)                         │  │
│  │                                                                        │  │
│  │              [◀ prev]            [next ▶]                              │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                 ░░░ CASE STUDY PREVIEW (white bg) ░░░                       │
│                                                                              │
│  OVERLINE: RESULTS                                                           │
│  "See What's Possible"                                                       │
│                                                                              │
│  ┌──────────────────────────────┐  ┌──────────────────────────────┐        │
│  │                              │  │                              │        │
│  │  [Client Logo]               │  │  [Client Logo]               │        │
│  │                              │  │                              │        │
│  │  ┌────────────────────────┐  │  │  ┌────────────────────────┐  │        │
│  │  │  +340% ROI  │  47%    │  │  │  │  +210% ROI  │  63%    │  │        │
│  │  │    lift     │ close   │  │  │  │    lift     │ close   │  │        │
│  │  │             │  rate   │  │  │  │             │  rate   │  │        │
│  │  └────────────────────────┘  │  │  └────────────────────────┘  │        │
│  │                              │  │                              │        │
│  │  "How Apex Insurance Group   │  │  "Regional MGA Cuts Cost     │        │
│  │   Tripled Their ROI with     │  │   Per Acquisition by 58%     │        │
│  │   Risk Segmentation"         │  │   with Quality Scoring"      │        │
│  │                              │  │                              │        │
│  │  Auto Insurance · Q4 2025    │  │  Commercial · Q1 2026        │        │
│  │                              │  │                              │        │
│  │  [Read Case Study →]         │  │  [Read Case Study →]         │        │
│  │                              │  │                              │        │
│  └──────────────────────────────┘  └──────────────────────────────┘        │
│                                                                              │
│                      [View All Case Studies →]                               │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                  ░░░ FINAL CTA SECTION (navy bg) ░░░                        │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │           Ready to Make Every Lead Count?                              │  │
│  │                                                                        │  │
│  │     See how The Switchboard's risk segmentation                        │  │
│  │     intelligence can transform your acquisition economics.             │  │
│  │                                                                        │  │
│  │          [ Request a Demo ]    [ Contact Sales ]                       │  │
│  │           (white btn)           (ghost white btn)                      │  │
│  │                                                                        │  │
│  │     ✓ No commitment   ✓ 30-min overview   ✓ Custom ROI analysis       │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ [FOOTER - see below]                                                         │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Solutions Overview

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [HEADER / NAV]                                                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                   ░░░ HERO (navy gradient, shorter) ░░░                      │
│                                                                              │
│  Breadcrumb: Home > Solutions                                                │
│                                                                              │
│  OVERLINE: OUR PLATFORM                                                      │
│                                                                              │
│        Complete Insurance Intelligence,                                      │
│        One Integrated Platform                                               │
│                                                                              │
│  From data enrichment to delivery — every layer of intelligence              │
│  you need to acquire customers profitably.                                   │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                ░░░ INTERACTIVE PLATFORM DIAGRAM ░░░                          │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │   ┌──────────┐                                       ┌──────────┐    │  │
│  │   │ SOURCES  │                                       │ DELIVERY │    │  │
│  │   │          │     ┌──────────────────────────┐      │          │    │  │
│  │   │ ○ Forms  │     │                          │      │ ○ API    │    │  │
│  │   │ ○ APIs   │────→│    THE SWITCHBOARD       │─────→│ ○ Feed   │    │  │
│  │   │ ○ Partner│     │                          │      │ ○ Batch  │    │  │
│  │   │ ○ Network│     │  [Enrich] → [Score] →    │      │ ○ Real-  │    │  │
│  │   │          │     │  [Route]  → [Price]      │      │   time   │    │  │
│  │   └──────────┘     │                          │      └──────────┘    │  │
│  │                    └──────────────────────────┘                       │  │
│  │                              ↕                                        │  │
│  │                    ┌──────────────────┐                               │  │
│  │                    │  FEEDBACK LOOP   │                               │  │
│  │                    │  Analytics ←→ ML │                               │  │
│  │                    └──────────────────┘                               │  │
│  │                                                                        │  │
│  │  (Click any node to jump to that solution page)                        │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│              ░░░ SOLUTION CARDS (full width, alternating) ░░░               │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  ┌─────────────────────────┐  ┌─────────────────────────────────────┐ │  │
│  │  │                         │  │                                     │ │  │
│  │  │  [Solution visual /     │  │  01 — DATA ENRICHMENT              │ │  │
│  │  │   screenshot /          │  │                                     │ │  │
│  │  │   illustration]         │  │  Turn thin records into rich        │ │  │
│  │  │                         │  │  profiles with 90GB+ of Equifax     │ │  │
│  │  │                         │  │  data merged in real-time.          │ │  │
│  │  │                         │  │                                     │ │  │
│  │  │                         │  │  ✓ Demographic enrichment           │ │  │
│  │  │                         │  │  ✓ Financial indicators             │ │  │
│  │  │                         │  │  ✓ Behavioral signals               │ │  │
│  │  │                         │  │  ✓ Property & vehicle data          │ │  │
│  │  │                         │  │                                     │ │  │
│  │  │                         │  │  [Learn More →]                     │ │  │
│  │  │                         │  │                                     │ │  │
│  │  └─────────────────────────┘  └─────────────────────────────────────┘ │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  ┌─────────────────────────────────────┐  ┌─────────────────────────┐ │  │
│  │  │                                     │  │                         │ │  │
│  │  │  02 — RISK SEGMENTATION             │  │  [Solution visual]      │ │  │
│  │  │                                     │  │                         │ │  │
│  │  │  Categorize every prospect by risk  │  │                         │ │  │
│  │  │  profile for precision targeting    │  │                         │ │  │
│  │  │  and optimal carrier matching.      │  │                         │ │  │
│  │  │                                     │  │                         │ │  │
│  │  │  ✓ Multi-factor risk scoring        │  │                         │ │  │
│  │  │  ✓ Carrier appetite matching        │  │                         │ │  │
│  │  │  ✓ Real-time segmentation           │  │                         │ │  │
│  │  │  ✓ Custom risk models               │  │                         │ │  │
│  │  │                                     │  │                         │ │  │
│  │  │  [Learn More →]                     │  │                         │ │  │
│  │  │                                     │  │                         │ │  │
│  │  └─────────────────────────────────────┘  └─────────────────────────┘ │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  (... same alternating pattern for Quality Scoring, Analytics,               │
│       Decision Engine ...)                                                   │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                ░░░ COMPARISON TABLE (gray bg) ░░░                            │
│                                                                              │
│  "Why The Switchboard?"                                                      │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  Feature              The Switchboard    Typical Lead Vendor  DIY      │  │
│  │  ─────────────────────────────────────────────────────────────────     │  │
│  │  Real-time enrichment      ✓                  ✗               ✗       │  │
│  │  Risk segmentation         ✓                  ✗               ~       │  │
│  │  ML quality scoring        ✓                  ~               ✗       │  │
│  │  Carrier matching          ✓                  ✗               ✗       │  │
│  │  Closed-loop analytics     ✓                  ✗               ~       │  │
│  │  <250ms processing         ✓                  ✗               ✗       │  │
│  │  Custom risk models        ✓                  ✗               ~       │  │
│  │  API + Feed delivery       ✓                  ~               ~       │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ [TESTIMONIAL SECTION]                                                        │
├──────────────────────────────────────────────────────────────────────────────┤
│ [FINAL CTA]                                                                  │
├──────────────────────────────────────────────────────────────────────────────┤
│ [FOOTER]                                                                     │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Individual Solution Page
*Example: Risk Segmentation*

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [HEADER / NAV]                                                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                      ░░░ HERO (navy gradient) ░░░                            │
│                                                                              │
│  Breadcrumb: Home > Solutions > Risk Segmentation                            │
│                                                                              │
│  ┌─────────────────────────────────┐  ┌──────────────────────────────────┐  │
│  │                                 │  │                                  │  │
│  │  OVERLINE: RISK SEGMENTATION    │  │  [Product screenshot /           │  │
│  │                                 │  │   dashboard showing risk         │  │
│  │  Know Your Risk Before          │  │   categories and scoring]        │  │
│  │  You Quote                      │  │                                  │  │
│  │                                 │  │                                  │  │
│  │  Categorize every prospect by   │  │                                  │  │
│  │  risk profile — so you target   │  │                                  │  │
│  │  the right people, match the    │  │                                  │  │
│  │  right carrier, and price for   │  │                                  │  │
│  │  profit.                        │  │                                  │  │
│  │                                 │  │                                  │  │
│  │  [ Request a Demo ]             │  │                                  │  │
│  │                                 │  │                                  │  │
│  └─────────────────────────────────┘  └──────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                    ░░░ KEY METRICS BAR ░░░                                   │
│                                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │  47%         │  │  <250ms      │  │  12+         │  │  99.7%       │    │
│  │  Higher      │  │  Scoring     │  │  Risk        │  │  Uptime      │    │
│  │  Close Rate  │  │  Speed       │  │  Categories  │  │              │    │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘    │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                  ░░░ THE PROBLEM (white bg) ░░░                              │
│                                                                              │
│  OVERLINE: THE CHALLENGE                                                     │
│                                                                              │
│  "Buying leads without risk intelligence is                                  │
│   like underwriting without data."                                           │
│                                                                              │
│  ┌────────────────────┐  ┌────────────────────┐  ┌────────────────────┐    │
│  │                    │  │                    │  │                    │    │
│  │  [✗ icon]          │  │  [✗ icon]          │  │  [✗ icon]          │    │
│  │                    │  │                    │  │                    │    │
│  │  Adverse           │  │  Wasted Spend      │  │  Missed            │    │
│  │  Selection         │  │  on Low-Intent     │  │  Opportunities     │    │
│  │                    │  │  Prospects          │  │                    │    │
│  │  High-risk leads   │  │  60% of leads      │  │  Best prospects    │    │
│  │  drain close       │  │  never convert.    │  │  go to competitors │    │
│  │  rates and lift    │  │  You're paying     │  │  who reach them    │    │
│  │  loss ratios.      │  │  for all of them.  │  │  first.            │    │
│  │                    │  │                    │  │                    │    │
│  └────────────────────┘  └────────────────────┘  └────────────────────┘    │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                  ░░░ THE SOLUTION (gray bg) ░░░                              │
│                                                                              │
│  OVERLINE: HOW IT WORKS                                                      │
│  "Risk Segmentation in Three Steps"                                          │
│                                                                              │
│      ┌─────────┐          ┌─────────┐          ┌─────────┐                 │
│      │   (1)   │          │   (2)   │          │   (3)   │                 │
│      │  DATA   │  ─────→  │  SCORE  │  ─────→  │  MATCH  │                 │
│      │  IN     │          │         │          │  & ROUTE │                 │
│      └─────────┘          └─────────┘          └─────────┘                 │
│                                                                              │
│      Lead enters with     ML models analyze     Scored lead matched         │
│      basic info (name,    50+ risk factors      to optimal carrier          │
│      zip, DOB, vehicle)   and assign risk       appetite and routed         │
│                           category (A-F)        to best buyer               │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                ░░░ FEATURES DETAIL (white bg) ░░░                            │
│                                                                              │
│  OVERLINE: CAPABILITIES                                                      │
│                                                                              │
│  ┌─────────────────────────────┐  ┌──────────────────────────────────────┐  │
│  │                             │  │                                      │  │
│  │  [screenshot / visual]      │  │  Multi-Factor Risk Scoring           │  │
│  │                             │  │                                      │  │
│  │                             │  │  Our models analyze 50+ data points  │  │
│  │                             │  │  including credit indicators, driver │  │
│  │                             │  │  history, property characteristics,  │  │
│  │                             │  │  and behavioral signals to assign    │  │
│  │                             │  │  granular risk categories.           │  │
│  │                             │  │                                      │  │
│  │                             │  │  ✓ 12+ risk categories (A through F │  │
│  │                             │  │    with sub-tiers)                   │  │
│  │                             │  │  ✓ Real-time scoring (<250ms)        │  │
│  │                             │  │  ✓ Custom model training per client  │  │
│  │                             │  │  ✓ Explainable scoring factors       │  │
│  │                             │  │                                      │  │
│  └─────────────────────────────┘  └──────────────────────────────────────┘  │
│                                                                              │
│  (... 2-3 more feature blocks, alternating image left/right ...)             │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│              ░░░ RELATED CASE STUDY (navy bg) ░░░                            │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  OVERLINE: CUSTOMER SUCCESS                                            │  │
│  │                                                                        │  │
│  │  ┌──────────┐                                                          │  │
│  │  │ [Client  │  "After implementing risk segmentation, we cut our       │  │
│  │  │  Logo]   │   cost per acquisition by 58% and improved our           │  │
│  │  └──────────┘   loss ratio by 12 points in the first 90 days."        │  │
│  │                                                                        │  │
│  │                 — James Chen, Director of Operations                    │  │
│  │                   Premier Insurance Partners                           │  │
│  │                                                                        │  │
│  │  ┌────────┐  ┌────────┐  ┌────────┐                                   │  │
│  │  │  -58%  │  │  +12pt │  │  3.2x  │                                   │  │
│  │  │  CPA   │  │  Loss  │  │  ROI   │                                   │  │
│  │  │        │  │  Ratio │  │        │                                   │  │
│  │  └────────┘  └────────┘  └────────┘                                   │  │
│  │                                                                        │  │
│  │  [Read Full Case Study →]                                              │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                      ░░░ FAQ SECTION ░░░                                     │
│                                                                              │
│  "Frequently Asked Questions"                                                │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │  ▼ How does risk segmentation differ from lead scoring?               │  │
│  │    Lead scoring rates intent. Risk segmentation rates insurability.    │  │
│  │    We do both — but segmentation is what prevents adverse selection.   │  │
│  ├────────────────────────────────────────────────────────────────────────┤  │
│  │  ▶ What data sources power the risk models?                           │  │
│  ├────────────────────────────────────────────────────────────────────────┤  │
│  │  ▶ Can I customize risk categories for my book of business?           │  │
│  ├────────────────────────────────────────────────────────────────────────┤  │
│  │  ▶ How fast is the scoring? Will it slow down my pipeline?            │  │
│  ├────────────────────────────────────────────────────────────────────────┤  │
│  │  ▶ Is the data FCRA and TCPA compliant?                               │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  (FAQ schema markup for Google rich snippets)                                │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ [FINAL CTA]                                                                  │
├──────────────────────────────────────────────────────────────────────────────┤
│ [FOOTER]                                                                     │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Industry Page
*Example: Auto Insurance*

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [HEADER / NAV]                                                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                      ░░░ HERO (navy gradient) ░░░                            │
│                                                                              │
│  Breadcrumb: Home > Industries > Auto Insurance                              │
│                                                                              │
│  OVERLINE: AUTO INSURANCE INTELLIGENCE                                       │
│                                                                              │
│        Risk-Scored Auto Leads That                                           │
│        Your Underwriters Will Love                                           │
│                                                                              │
│  Stop buying blind. Start buying intelligently — with driver history,        │
│  vehicle data, and carrier appetite matching built into every lead.          │
│                                                                              │
│  [ Request a Demo ]   [ See Auto Case Study ]                                │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                ░░░ INDUSTRY-SPECIFIC STATS ░░░                               │
│                                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │  2.1M+       │  │  38%         │  │  <200ms      │  │  15+         │    │
│  │  Auto leads  │  │  Better      │  │  Enrichment  │  │  Carrier     │    │
│  │  scored      │  │  loss ratio  │  │  time         │  │  integrations│    │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘    │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│             ░░░ AUTO-SPECIFIC ENRICHMENT DATA ░░░                            │
│                                                                              │
│  "What We Know About Every Auto Prospect"                                    │
│                                                                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐          │
│  │                  │  │                  │  │                  │          │
│  │  [car icon]      │  │  [shield icon]   │  │  [chart icon]    │          │
│  │                  │  │                  │  │                  │          │
│  │  Vehicle Data    │  │  Driver History   │  │  Risk Profile    │          │
│  │                  │  │                  │  │                  │          │
│  │  • Make/Model    │  │  • MVR indicators│  │  • Risk tier      │          │
│  │  • Year          │  │  • Claims history│  │    (A through F)  │          │
│  │  • VIN decode    │  │  • Coverage gaps │  │  • Predicted LTV  │          │
│  │  • Garaging ZIP  │  │  • License status│  │  • Conversion     │          │
│  │  • Current ins.  │  │  • Accident freq.│  │    probability   │          │
│  │                  │  │                  │  │  • Price point    │          │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘          │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│         ░░░ HOW IT WORKS FOR AUTO (process diagram) ░░░                     │
│                                                                              │
│  (Similar 4-step flow as homepage but with auto-specific labels)             │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│            ░░░ AUTO CASE STUDY HIGHLIGHT (navy bg) ░░░                      │
│                                                                              │
│  (Full-width case study card with auto-specific metrics)                     │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│            ░░░ "OTHER INDUSTRIES" CROSS-LINKS ░░░                           │
│                                                                              │
│  ┌─────────────────────┐ ┌─────────────────────┐ ┌─────────────────────┐   │
│  │  Health Insurance →  │ │  Commercial →        │ │  Life Insurance →   │   │
│  └─────────────────────┘ └─────────────────────┘ └─────────────────────┘   │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ [FINAL CTA] [FAQ SECTION] [FOOTER]                                           │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 5. About Page

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [HEADER / NAV]                                                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                      ░░░ HERO (navy gradient) ░░░                            │
│                                                                              │
│  OVERLINE: ABOUT THE SWITCHBOARD                                             │
│                                                                              │
│        We Built the Intelligence Layer                                       │
│        That Insurance Deserves                                               │
│                                                                              │
│  Founded by insurance operators who were tired of buying                     │
│  blind leads and watching margins erode.                                     │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                  ░░░ OUR STORY (2-column) ░░░                                │
│                                                                              │
│  ┌──────────────────────────────┐  ┌──────────────────────────────────────┐ │
│  │                              │  │                                      │ │
│  │  [Team photo /               │  │  THE PROBLEM WE SAW                  │ │
│  │   office photo /             │  │                                      │ │
│  │   founder photo]             │  │  Insurance customer acquisition was  │ │
│  │                              │  │  broken. Agencies bought leads       │ │
│  │                              │  │  blind — no risk data, no quality    │ │
│  │                              │  │  scoring, no intelligence.           │ │
│  │                              │  │                                      │ │
│  │                              │  │  THE SOLUTION WE BUILT               │ │
│  │                              │  │                                      │ │
│  │                              │  │  A platform that enriches, scores,   │ │
│  │                              │  │  and routes every lead with the      │ │
│  │                              │  │  same rigor that underwriters        │ │
│  │                              │  │  apply to policies.                  │ │
│  │                              │  │                                      │ │
│  └──────────────────────────────┘  └──────────────────────────────────────┘ │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                   ░░░ COMPANY METRICS BAR ░░░                                │
│                                                                              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│  │ Founded  │  │  10M+    │  │  50+     │  │  <250ms  │  │  99.9%   │     │
│  │ 2023     │  │  Leads   │  │  Agency  │  │ Avg      │  │  Uptime  │     │
│  │          │  │ Processed│  │ Partners │  │ Latency  │  │          │     │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────────┘     │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                  ░░░ OUR VALUES (3-col grid) ░░░                             │
│                                                                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐          │
│  │                  │  │                  │  │                  │          │
│  │  [icon]          │  │  [icon]          │  │  [icon]          │          │
│  │                  │  │                  │  │                  │          │
│  │  Data-Driven     │  │  Insurance-First │  │  Relentless      │          │
│  │  Decisions       │  │  Thinking        │  │  Optimization    │          │
│  │                  │  │                  │  │                  │          │
│  │  Every feature   │  │  Built by people │  │  We measure      │          │
│  │  we build is     │  │  who understand  │  │  everything and  │          │
│  │  validated by    │  │  loss ratios,    │  │  improve         │          │
│  │  measurable      │  │  adverse         │  │  constantly.     │          │
│  │  outcomes.       │  │  selection, and  │  │                  │          │
│  │                  │  │  carrier needs.  │  │                  │          │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘          │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                 ░░░ LEADERSHIP TEAM (grid) ░░░                               │
│                                                                              │
│  OVERLINE: OUR TEAM                                                          │
│  "The People Behind the Platform"                                            │
│                                                                              │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐           │
│  │            │  │            │  │            │  │            │           │
│  │  ┌──────┐ │  │  ┌──────┐ │  │  ┌──────┐ │  │  ┌──────┐ │           │
│  │  │[head-│ │  │  │[head-│ │  │  │[head-│ │  │  │[head-│ │           │
│  │  │ shot]│ │  │  │ shot]│ │  │  │ shot]│ │  │  │ shot]│ │           │
│  │  └──────┘ │  │  └──────┘ │  │  └──────┘ │  │  └──────┘ │           │
│  │            │  │            │  │            │  │            │           │
│  │  Name      │  │  Name      │  │  Name      │  │  Name      │           │
│  │  CEO &     │  │  CTO       │  │  VP of     │  │  VP of     │           │
│  │  Founder   │  │            │  │  Sales     │  │  Data      │           │
│  │            │  │            │  │            │  │  Science   │           │
│  │  [LinkedIn]│  │  [LinkedIn]│  │  [LinkedIn]│  │  [LinkedIn]│           │
│  │            │  │            │  │            │  │            │           │
│  │  Brief bio │  │  Brief bio │  │  Brief bio │  │  Brief bio │           │
│  │  2-3 lines │  │  2-3 lines │  │  2-3 lines │  │  2-3 lines │           │
│  │            │  │            │  │            │  │            │           │
│  └────────────┘  └────────────┘  └────────────┘  └────────────┘           │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│               ░░░ COMPLIANCE & SECURITY BAR ░░░                              │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  [SOC 2 badge]  [TCPA badge]  [Encrypted badge]  [CCPA badge]         │  │
│  │                                                                        │  │
│  │  "Your data security is our foundation.                                │  │
│  │   Learn more about our security practices →"                           │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ [CTA: "Join the 50+ agencies already growing with The Switchboard"]          │
├──────────────────────────────────────────────────────────────────────────────┤
│ [FOOTER]                                                                     │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Contact Page

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [HEADER / NAV]                                                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                      ░░░ HERO (navy, short) ░░░                              │
│                                                                              │
│        Let's Talk About Your Growth                                          │
│                                                                              │
│  Whether you're ready to start or just exploring,                            │
│  we'd love to understand your acquisition challenges.                        │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│             ░░░ CONTACT SECTION (2-column) ░░░                               │
│                                                                              │
│  ┌──────────────────────────────────┐  ┌──────────────────────────────────┐ │
│  │                                  │  │                                  │ │
│  │  MULTI-STEP FORM                 │  │  OTHER WAYS TO REACH US         │ │
│  │                                  │  │                                  │ │
│  │  Progress: [●●○○] Step 2 of 4   │  │  ┌─────────────────────────┐    │ │
│  │                                  │  │  │                         │    │ │
│  │  ┌──────────────────────────┐   │  │  │  [phone icon]           │    │ │
│  │  │                          │   │  │  │  Call Us                 │    │ │
│  │  │  STEP 1: YOUR INFO       │   │  │  │  (323) 471-6020         │    │ │
│  │  │                          │   │  │  │  Mon-Fri 8am-6pm MST    │    │ │
│  │  │  First Name *            │   │  │  │                         │    │ │
│  │  │  [________________]      │   │  │  └─────────────────────────┘    │ │
│  │  │                          │   │  │                                  │ │
│  │  │  Last Name *             │   │  │  ┌─────────────────────────┐    │ │
│  │  │  [________________]      │   │  │  │                         │    │ │
│  │  │                          │   │  │  │  [email icon]           │    │ │
│  │  │  Work Email *            │   │  │  │  Email Us               │    │ │
│  │  │  [________________]      │   │  │  │  hello@theswitchboard   │    │ │
│  │  │                          │   │  │  │  marketing.com          │    │ │
│  │  │  Phone                   │   │  │  │                         │    │ │
│  │  │  [________________]      │   │  │  └─────────────────────────┘    │ │
│  │  │                          │   │  │                                  │ │
│  │  │  [ Next Step → ]         │   │  │  ┌─────────────────────────┐    │ │
│  │  │                          │   │  │  │                         │    │ │
│  │  └──────────────────────────┘   │  │  │  [pin icon]             │    │ │
│  │                                  │  │  │  Visit Us               │    │ │
│  │  ┌──────────────────────────┐   │  │  │  1849 W 645 S           │    │ │
│  │  │                          │   │  │  │  Orem, Utah 84059       │    │ │
│  │  │  STEP 2: YOUR COMPANY    │   │  │  │                         │    │ │
│  │  │                          │   │  │  └─────────────────────────┘    │ │
│  │  │  Company Name *          │   │  │                                  │ │
│  │  │  [________________]      │   │  │  ┌─────────────────────────┐    │ │
│  │  │                          │   │  │  │                         │    │ │
│  │  │  Your Title *            │   │  │  │  [calendar icon]        │    │ │
│  │  │  [________________]      │   │  │  │  Prefer a Scheduled     │    │ │
│  │  │                          │   │  │  │  Call?                   │    │ │
│  │  │  Company Size *          │   │  │  │                         │    │ │
│  │  │  [▼ Select...        ]   │   │  │  │  [ Book a Demo → ]     │    │ │
│  │  │    1-10 employees        │   │  │  │                         │    │ │
│  │  │    11-50 employees       │   │  │  └─────────────────────────┘    │ │
│  │  │    51-200 employees      │   │  │                                  │ │
│  │  │    201-500 employees     │   │  │                                  │ │
│  │  │    500+ employees        │   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  │  [← Back] [ Next Step →]│   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  └──────────────────────────┘   │  │                                  │ │
│  │                                  │  │                                  │ │
│  │  ┌──────────────────────────┐   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  │  STEP 3: YOUR NEEDS      │   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  │  Insurance Lines *       │   │  │                                  │ │
│  │  │  (multi-select)          │   │  │                                  │ │
│  │  │  ☐ Auto                  │   │  │                                  │ │
│  │  │  ☐ Health / ACA          │   │  │                                  │ │
│  │  │  ☐ Commercial / Business │   │  │                                  │ │
│  │  │  ☐ Life / Annuity        │   │  │                                  │ │
│  │  │  ☐ Medicare              │   │  │                                  │ │
│  │  │  ☐ Other                 │   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  │  Monthly Lead Volume *   │   │  │                                  │ │
│  │  │  [▼ Select...        ]   │   │  │                                  │ │
│  │  │    < 500 leads           │   │  │                                  │ │
│  │  │    500 - 2,000           │   │  │                                  │ │
│  │  │    2,000 - 10,000        │   │  │                                  │ │
│  │  │    10,000+               │   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  │  Biggest Challenge *     │   │  │                                  │ │
│  │  │  [▼ Select...        ]   │   │  │                                  │ │
│  │  │    Lead quality          │   │  │                                  │ │
│  │  │    Cost per acquisition  │   │  │                                  │ │
│  │  │    Conversion rates      │   │  │                                  │ │
│  │  │    Compliance / risk     │   │  │                                  │ │
│  │  │    Scaling volume        │   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  │  [← Back] [ Next Step →]│   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  └──────────────────────────┘   │  │                                  │ │
│  │                                  │  │                                  │ │
│  │  ┌──────────────────────────┐   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  │  STEP 4: CONFIRM         │   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  │  Additional Message      │   │  │                                  │ │
│  │  │  [                   ]   │   │  │                                  │ │
│  │  │  [                   ]   │   │  │                                  │ │
│  │  │  [                   ]   │   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  │  ☑ TCPA consent + link   │   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  │  [← Back] [ Submit  ]   │   │  │                                  │ │
│  │  │           (primary btn)  │   │  │                                  │ │
│  │  │                          │   │  │                                  │ │
│  │  └──────────────────────────┘   │  │                                  │ │
│  │                                  │  │                                  │ │
│  └──────────────────────────────────┘  └──────────────────────────────────┘ │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ [FOOTER]                                                                     │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Demo Request Page

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [HEADER / NAV]                                                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                      ░░░ HERO (navy, short) ░░░                              │
│                                                                              │
│        See The Switchboard in Action                                         │
│                                                                              │
│  30-minute personalized walkthrough. No commitment.                          │
│  We'll show you how risk segmentation can transform your numbers.            │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│           ░░░ DEMO BOOKING (2-column layout) ░░░                             │
│                                                                              │
│  ┌──────────────────────────────────┐  ┌──────────────────────────────────┐ │
│  │                                  │  │                                  │ │
│  │  WHAT YOU'LL SEE:                │  │  ┌──────────────────────────┐   │ │
│  │                                  │  │  │                          │   │ │
│  │  ✓ Live enrichment demo with     │  │  │   [CAL.COM / CALENDLY   │   │ │
│  │    real data                     │  │  │    EMBED]                │   │ │
│  │                                  │  │  │                          │   │ │
│  │  ✓ Risk segmentation in action   │  │  │   Select a date & time  │   │ │
│  │    — watch a lead get scored     │  │  │                          │   │ │
│  │    in <250ms                     │  │  │   ┌─────────────────┐   │   │ │
│  │                                  │  │  │   │  March 2026     │   │   │ │
│  │  ✓ ROI projection based on       │  │  │   │                 │   │   │ │
│  │    your current volume           │  │  │   │  M  T  W  T  F  │   │   │ │
│  │                                  │  │  │   │ 10 11 12 13 14  │   │   │ │
│  │  ✓ Q&A with our team            │  │  │   │ 17 18 19 20 21  │   │   │ │
│  │                                  │  │  │   │ 24 25 26 27 28  │   │   │ │
│  │  ──────────────────              │  │  │   └─────────────────┘   │   │ │
│  │                                  │  │  │                          │   │ │
│  │  WHO SHOULD ATTEND:              │  │  │   Available times:       │   │ │
│  │                                  │  │  │   ○ 9:00 AM MST         │   │ │
│  │  • Agency owners & principals    │  │  │   ○ 10:30 AM MST        │   │ │
│  │  • VP of Marketing / Growth      │  │  │   ○ 1:00 PM MST         │   │ │
│  │  • Operations leadership         │  │  │   ○ 2:30 PM MST         │   │ │
│  │  • Anyone evaluating lead        │  │  │   ○ 4:00 PM MST         │   │ │
│  │    vendors or data providers     │  │  │                          │   │ │
│  │                                  │  │  │   [Confirm Booking →]    │   │ │
│  │                                  │  │  │                          │   │ │
│  │                                  │  │  └──────────────────────────┘   │ │
│  │                                  │  │                                  │ │
│  └──────────────────────────────────┘  └──────────────────────────────────┘ │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│            ░░░ SOCIAL PROOF (quick trust bar) ░░░                            │
│                                                                              │
│  "Join 50+ agencies who started with a demo"                                 │
│  [Logo] [Logo] [Logo] [Logo] [Logo] [Logo]                                  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ [FOOTER]                                                                     │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Resources Hub

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [HEADER / NAV]                                                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                      ░░░ HERO (navy, short) ░░░                              │
│                                                                              │
│        Insights & Intelligence                                               │
│                                                                              │
│  Industry research, best practices, and customer success stories             │
│  to help you acquire customers more profitably.                              │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                  ░░░ FILTER BAR (sticky) ░░░                                 │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  [All]  [Blog]  [Case Studies]  [Whitepapers]  [Webinars]             │  │
│  │                                                                        │  │
│  │  Industry: [▼ All Industries]    Topic: [▼ All Topics]                │  │
│  │                                                                        │  │
│  │  [🔍 Search resources...]                                              │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                ░░░ FEATURED RESOURCE (full width) ░░░                        │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  ┌────────────────────────────┐  ┌──────────────────────────────────┐ │  │
│  │  │                            │  │                                  │ │  │
│  │  │  [Featured image /         │  │  FEATURED · WHITEPAPER           │ │  │
│  │  │   report cover]            │  │                                  │ │  │
│  │  │                            │  │  "2026 State of Insurance        │ │  │
│  │  │                            │  │   Lead Intelligence Report"      │ │  │
│  │  │                            │  │                                  │ │  │
│  │  │                            │  │  Analysis of 10M+ leads across   │ │  │
│  │  │                            │  │  auto, health, and commercial    │ │  │
│  │  │                            │  │  lines. Key findings on risk     │ │  │
│  │  │                            │  │  segmentation ROI.               │ │  │
│  │  │                            │  │                                  │ │  │
│  │  │                            │  │  [ Download Free Report → ]      │ │  │
│  │  │                            │  │                                  │ │  │
│  │  └────────────────────────────┘  └──────────────────────────────────┘ │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                ░░░ RESOURCE GRID (3-column) ░░░                              │
│                                                                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐          │
│  │                  │  │                  │  │                  │          │
│  │  [image]         │  │  [image]         │  │  [client logo]   │          │
│  │                  │  │                  │  │                  │          │
│  │  BLOG            │  │  CASE STUDY      │  │  WEBINAR         │          │
│  │                  │  │                  │  │                  │          │
│  │  "5 Risk         │  │  "How Apex       │  │  "Live Demo:     │          │
│  │   Segmentation   │  │   Insurance      │  │   Risk           │          │
│  │   Strategies     │  │   Tripled ROI"   │  │   Segmentation   │          │
│  │   for 2026"      │  │                  │  │   in Action"     │          │
│  │                  │  │  +340% ROI lift   │  │                  │          │
│  │  Mar 5, 2026     │  │                  │  │  45 min          │          │
│  │  8 min read      │  │  Auto Insurance  │  │  Mar 15, 2026    │          │
│  │                  │  │                  │  │                  │          │
│  │  [Read →]        │  │  [Read →]        │  │  [Watch →]       │          │
│  │                  │  │                  │  │                  │          │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘          │
│                                                                              │
│  (... 6-9 more cards in grid ...)                                            │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │              [← Previous]  1  2  3  ...  8  [Next →]                  │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│             ░░░ NEWSLETTER SIGNUP (gray bg) ░░░                              │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │       Get Intelligence Delivered                                       │  │
│  │                                                                        │  │
│  │  Industry insights, product updates, and best practices                │  │
│  │  straight to your inbox. No spam. Unsubscribe anytime.                 │  │
│  │                                                                        │  │
│  │  [email@company.com          ]  [ Subscribe ]                         │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ [FOOTER]                                                                     │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 9. Blog Post

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [HEADER / NAV]                                                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Breadcrumb: Home > Resources > Blog > [Post Title]                          │
│                                                                              │
│  ┌──────────────────────────────────────────────┐  ┌──────────────────────┐ │
│  │                                              │  │                      │ │
│  │  RISK SEGMENTATION · 8 MIN READ              │  │  SIDEBAR             │ │
│  │                                              │  │                      │ │
│  │  5 Risk Segmentation Strategies              │  │  ┌────────────────┐ │ │
│  │  That Cut CPA by 40%+ in 2026                │  │  │                │ │ │
│  │                                              │  │  │  TABLE OF      │ │ │
│  │  ┌──────┐                                    │  │  │  CONTENTS      │ │ │
│  │  │[photo│  By Sarah Chen · Mar 5, 2026       │  │  │                │ │ │
│  │  │  ]   │  VP of Data Science                │  │  │  1. Strategy 1 │ │ │
│  │  └──────┘                                    │  │  │  2. Strategy 2 │ │ │
│  │                                              │  │  │  3. Strategy 3 │ │ │
│  │  ┌────────────────────────────────────────┐  │  │  │  4. Strategy 4 │ │ │
│  │  │                                        │  │  │  │  5. Strategy 5 │ │ │
│  │  │  [Featured hero image]                 │  │  │  │                │ │ │
│  │  │                                        │  │  │  └────────────────┘ │ │
│  │  └────────────────────────────────────────┘  │  │                      │ │
│  │                                              │  │  ┌────────────────┐ │ │
│  │  ## Introduction                              │  │  │                │ │ │
│  │                                              │  │  │  CTA BOX       │ │ │
│  │  Rich text content with proper heading        │  │  │                │ │ │
│  │  hierarchy (H2, H3), bullet points,           │  │  │  "Want to see │ │ │
│  │  blockquotes, images, code blocks.            │  │  │   risk scoring│ │ │
│  │                                              │  │  │   in action?"  │ │ │
│  │  ## Strategy 1: [Title]                       │  │  │                │ │ │
│  │                                              │  │  │ [Request Demo] │ │ │
│  │  Paragraph text with data, examples,          │  │  │                │ │ │
│  │  and actionable advice...                     │  │  └────────────────┘ │ │
│  │                                              │  │                      │ │
│  │  > "Blockquote from industry expert           │  │  ┌────────────────┐ │ │
│  │  > or data point"                             │  │  │                │ │ │
│  │                                              │  │  │  RELATED POSTS │ │ │
│  │  (... full article content ...)               │  │  │                │ │ │
│  │                                              │  │  │  [img] Title 1 │ │ │
│  │  ────────────────────────────────────         │  │  │                │ │ │
│  │                                              │  │  │  [img] Title 2 │ │ │
│  │  SHARE: [Twitter] [LinkedIn] [Copy Link]      │  │  │                │ │ │
│  │                                              │  │  │  [img] Title 3 │ │ │
│  │  ────────────────────────────────────         │  │  │                │ │ │
│  │                                              │  │  └────────────────┘ │ │
│  │  TAGS: [Risk Segmentation] [Auto] [ROI]       │  │                      │ │
│  │                                              │  │  ┌────────────────┐ │ │
│  │  ┌──────────────────────────────────────┐    │  │  │                │ │ │
│  │  │  AUTHOR BIO                          │    │  │  │  NEWSLETTER    │ │ │
│  │  │  ┌──────┐                            │    │  │  │                │ │ │
│  │  │  │[photo│  Sarah Chen                │    │  │  │  [email     ]  │ │ │
│  │  │  │  ]   │  VP of Data Science        │    │  │  │  [Subscribe ]  │ │ │
│  │  │  └──────┘  10 years in insurance     │    │  │  │                │ │ │
│  │  │           analytics. Formerly...     │    │  │  └────────────────┘ │ │
│  │  │           [LinkedIn] [Twitter]       │    │  │                      │ │
│  │  └──────────────────────────────────────┘    │  │                      │ │
│  │                                              │  │                      │ │
│  └──────────────────────────────────────────────┘  └──────────────────────┘ │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│              ░░░ RELATED ARTICLES (3-col grid) ░░░                           │
│                                                                              │
│  "More From Our Blog"                                                        │
│                                                                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐          │
│  │  [Blog Card]     │  │  [Blog Card]     │  │  [Blog Card]     │          │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘          │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ [CTA SECTION] [FOOTER]                                                       │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 10. Case Study

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [HEADER / NAV]                                                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Breadcrumb: Home > Resources > Case Studies > [Title]                       │
│                                                                              │
│                    ░░░ HERO (navy gradient) ░░░                              │
│                                                                              │
│  OVERLINE: CASE STUDY · AUTO INSURANCE                                       │
│                                                                              │
│  ┌─────────────────────┐                                                     │
│  │  [Client Logo]      │  How Apex Insurance Group                           │
│  │  Apex Insurance     │  Tripled Their ROI with                             │
│  └─────────────────────┘  Risk Segmentation                                  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                  ░░░ KEY RESULTS BAR ░░░                                     │
│                                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │              │  │              │  │              │  │              │    │
│  │   +340%      │  │   47%        │  │   -58%       │  │   90         │    │
│  │   ROI Lift   │  │   Close Rate │  │   CPA        │  │   Days to    │    │
│  │              │  │   Increase   │  │   Reduction  │  │   Full ROI   │    │
│  │              │  │              │  │              │  │              │    │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘    │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────────────────────────────┐  ┌──────────────────────┐ │
│  │                                              │  │                      │ │
│  │  ## The Company                               │  │  SIDEBAR             │ │
│  │                                              │  │                      │ │
│  │  Apex Insurance Group is a mid-size agency   │  │  ┌────────────────┐ │ │
│  │  with 85 agents across 12 locations in the   │  │  │ QUICK FACTS    │ │ │
│  │  Southeast, specializing in personal auto    │  │  │                │ │ │
│  │  and homeowners insurance.                   │  │  │ Industry:      │ │ │
│  │                                              │  │  │ Auto Insurance │ │ │
│  │  ─────────────────────────────────           │  │  │                │ │ │
│  │                                              │  │  │ Company Size:  │ │ │
│  │  ## The Challenge                             │  │  │ 85 agents      │ │ │
│  │                                              │  │  │                │ │ │
│  │  Apex was spending $180K/month on leads      │  │  │ Lead Volume:   │ │ │
│  │  with no risk intelligence. Close rates      │  │  │ 8,000/month    │ │ │
│  │  were declining, CPA was rising, and loss    │  │  │                │ │ │
│  │  ratios were trending the wrong direction.   │  │  │ Products:      │ │ │
│  │                                              │  │  │ Data Enrichment│ │ │
│  │  Key pain points:                             │  │  │ Risk Segment.  │ │ │
│  │  • 60% of leads never quoted                  │  │  │ Quality Scoring│ │ │
│  │  • No visibility into lead risk profiles      │  │  │                │ │ │
│  │  • Carriers complaining about loss ratios     │  │  │ Timeline:      │ │ │
│  │                                              │  │  │ 90 days        │ │ │
│  │  ─────────────────────────────────           │  │  │                │ │ │
│  │                                              │  │  └────────────────┘ │ │
│  │  ## The Solution                              │  │                      │ │
│  │                                              │  │  ┌────────────────┐ │ │
│  │  The Switchboard implemented:                 │  │  │                │ │ │
│  │                                              │  │  │  "The quality  │ │ │
│  │  1. Real-time Equifax data enrichment on      │  │  │   of leads     │ │ │
│  │     every inbound lead                        │  │  │   improved     │ │ │
│  │  2. Risk segmentation scoring (A-F tiers)     │  │  │   overnight.   │ │ │
│  │  3. Carrier appetite matching for auto        │  │  │   Our agents   │ │ │
│  │  4. Dynamic routing based on risk + geo       │  │  │   could feel   │ │ │
│  │                                              │  │  │   the          │ │ │
│  │  ─────────────────────────────────           │  │  │   difference." │ │ │
│  │                                              │  │  │                │ │ │
│  │  ## The Results                                │  │  │  — VP of Sales │ │ │
│  │                                              │  │  │    Apex Ins.   │ │ │
│  │  ┌──────────────────────────────────────┐    │  │  │                │ │ │
│  │  │                                      │    │  │  └────────────────┘ │ │
│  │  │  BEFORE           AFTER              │    │  │                      │ │
│  │  │  ──────           ─────              │    │  │  ┌────────────────┐ │ │
│  │  │  CPA: $142        CPA: $59           │    │  │  │                │ │ │
│  │  │  Close: 12%       Close: 18%         │    │  │  │  GET SIMILAR   │ │ │
│  │  │  Loss ratio: 68%  Loss ratio: 56%    │    │  │  │  RESULTS       │ │ │
│  │  │  ROI: 1.8x        ROI: 6.1x         │    │  │  │                │ │ │
│  │  │                                      │    │  │  │ [Request Demo] │ │ │
│  │  └──────────────────────────────────────┘    │  │  │                │ │ │
│  │                                              │  │  └────────────────┘ │ │
│  │                                              │  │                      │ │
│  └──────────────────────────────────────────────┘  └──────────────────────┘ │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│              ░░░ MORE CASE STUDIES (3-col) ░░░                               │
│                                                                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐          │
│  │ [Case Study Card]│  │ [Case Study Card]│  │ [Case Study Card]│          │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘          │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ [CTA SECTION] [FOOTER]                                                       │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 11. Security Page

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [HEADER / NAV]                                                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                    ░░░ HERO (navy gradient) ░░░                              │
│                                                                              │
│  OVERLINE: SECURITY & COMPLIANCE                                             │
│                                                                              │
│        Your Data Security Is Our Foundation                                  │
│                                                                              │
│  Enterprise-grade security built for the insurance industry's                │
│  most stringent compliance requirements.                                     │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                 ░░░ COMPLIANCE BADGES ░░░                                    │
│                                                                              │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐           │
│  │            │  │            │  │            │  │            │           │
│  │ [SOC 2    │  │ [TCPA      │  │ [CCPA      │  │ [GDPR      │           │
│  │  Type II  │  │  Compliant]│  │  Compliant]│  │  Ready]    │           │
│  │  badge]   │  │            │  │            │  │            │           │
│  │            │  │            │  │            │  │            │           │
│  └────────────┘  └────────────┘  └────────────┘  └────────────┘           │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐          │
│  │                  │  │                  │  │                  │          │
│  │  [lock icon]     │  │  [server icon]   │  │  [eye icon]      │          │
│  │                  │  │                  │  │                  │          │
│  │  Encryption      │  │  Infrastructure  │  │  Access Control  │          │
│  │                  │  │                  │  │                  │          │
│  │  AES-256 at rest │  │  SOC 2 compliant │  │  Role-based      │          │
│  │  TLS 1.3 in      │  │  hosting on      │  │  access with     │          │
│  │  transit. Zero   │  │  Vercel Edge     │  │  MFA. Audit      │          │
│  │  plaintext PII.  │  │  Network. 99.99% │  │  logs on every   │          │
│  │                  │  │  SLA uptime.     │  │  data access.    │          │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘          │
│                                                                              │
│  (... more security detail sections ...)                                     │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  Have security questions? Contact our security team:                    │  │
│  │  security@theswitchboardmarketing.com                                  │  │
│  │                                                                        │  │
│  │  [ Download Security Whitepaper ]   [ Request SOC 2 Report ]           │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
├──────────────────────────────────────────────────────────────────────────────┤
│ [FOOTER]                                                                     │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 13. Footer

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│                      ░░░ FOOTER (navy bg) ░░░                                │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  ┌────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐  │  │
│  │  │            │  │              │  │              │  │            │  │  │
│  │  │ [LOGO]     │  │ SOLUTIONS    │  │ RESOURCES    │  │ COMPANY    │  │  │
│  │  │            │  │              │  │              │  │            │  │  │
│  │  │ The        │  │ Data         │  │ Blog         │  │ About      │  │  │
│  │  │ intelligence│  │ Enrichment   │  │ Case Studies │  │ Careers    │  │  │
│  │  │ layer      │  │ Risk         │  │ Whitepapers  │  │ Contact    │  │  │
│  │  │ behind     │  │ Segmentation │  │ Webinars     │  │ Security   │  │  │
│  │  │ profitable │  │ Quality      │  │              │  │ Privacy    │  │  │
│  │  │ insurance  │  │ Scoring      │  │ INDUSTRIES   │  │ Terms      │  │  │
│  │  │ growth.    │  │ Analytics    │  │              │  │            │  │  │
│  │  │            │  │ Decision     │  │ Auto         │  │ GET IN     │  │  │
│  │  │ [LinkedIn] │  │ Engine       │  │ Health       │  │ TOUCH      │  │  │
│  │  │ [Twitter]  │  │              │  │ Commercial   │  │            │  │  │
│  │  │            │  │              │  │ Life         │  │ (323)      │  │  │
│  │  │            │  │              │  │              │  │ 471-6020   │  │  │
│  │  │            │  │              │  │              │  │            │  │  │
│  │  │            │  │              │  │              │  │ 1849 W     │  │  │
│  │  │            │  │              │  │              │  │ 645 S      │  │  │
│  │  │            │  │              │  │              │  │ Orem, UT   │  │  │
│  │  │            │  │              │  │              │  │ 84059      │  │  │
│  │  │            │  │              │  │              │  │            │  │  │
│  │  └────────────┘  └──────────────┘  └──────────────┘  └────────────┘  │  │
│  │                                                                        │  │
│  │  ────────────────────────────────────────────────────────────────────  │  │
│  │                                                                        │  │
│  │  NEWSLETTER: Get intelligence delivered.                               │  │
│  │                                                                        │  │
│  │  [email@company.com                    ]  [ Subscribe ]                │  │
│  │                                                                        │  │
│  │  ────────────────────────────────────────────────────────────────────  │  │
│  │                                                                        │  │
│  │  © 2026 The Switchboard. All rights reserved.                          │  │
│  │                                                                        │  │
│  │  [SOC 2 badge]  [TCPA badge]  [Encrypted badge]                       │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 14. Mobile Variants

### Mobile Header (< 768px)
```
┌────────────────────────────┐
│                            │
│  [LOGO]            [☰]    │
│                            │
├────────────────────────────┤
│  (hamburger opens:)        │
│                            │
│  Solutions          [▶]   │
│  ─────────────────────     │
│  Industries         [▶]   │
│  ─────────────────────     │
│  Resources          [▶]   │
│  ─────────────────────     │
│  About                     │
│  ─────────────────────     │
│  Contact                   │
│  ─────────────────────     │
│                            │
│  [ Request a Demo ]        │
│  (full-width primary btn)  │
│                            │
│  (323) 471-6020            │
│                            │
└────────────────────────────┘
```

### Mobile Homepage Hero
```
┌────────────────────────────┐
│                            │
│  INSURANCE INTELLIGENCE    │
│  PLATFORM                  │
│                            │
│  The Intelligence          │
│  Layer Behind              │
│  Profitable                │
│  Insurance Growth          │
│                            │
│  Transform raw prospect    │
│  data into risk-scored,    │
│  conversion-predicted      │
│  opportunities.            │
│                            │
│  [ Request a Demo    ]     │
│  (full-width)              │
│                            │
│  [ See How It Works  ]     │
│  (full-width ghost)        │
│                            │
│  ┌──────────────────────┐  │
│  │                      │  │
│  │  [Hero image -       │  │
│  │   stacks below       │  │
│  │   on mobile]         │  │
│  │                      │  │
│  └──────────────────────┘  │
│                            │
│  Trusted by 50+ insurance  │
│  agencies processing       │
│  10,000+ leads daily       │
│                            │
└────────────────────────────┘
```

### Mobile Stats
```
┌────────────────────────────┐
│                            │
│  ┌──────────┐ ┌──────────┐│
│  │  10M+    │ │  <250    ││
│  │  Leads   │ │  ms      ││
│  │ Processed│ │ Enrichment││
│  └──────────┘ └──────────┘│
│                            │
│  ┌──────────┐ ┌──────────┐│
│  │  340%    │ │  50+     ││
│  │  Avg ROI │ │  Agency  ││
│  │  Lift    │ │  Partners││
│  └──────────┘ └──────────┘│
│                            │
└────────────────────────────┘
```

### Mobile Contact Form (Multi-step)
```
┌────────────────────────────┐
│                            │
│  Step 1 of 4               │
│  [●●○○]                   │
│                            │
│  YOUR INFO                 │
│                            │
│  First Name *              │
│  [____________________]    │
│                            │
│  Last Name *               │
│  [____________________]    │
│                            │
│  Work Email *              │
│  [____________________]    │
│                            │
│  Phone                     │
│  [____________________]    │
│                            │
│  [ Next Step →         ]   │
│  (full-width btn)          │
│                            │
│  ─────── or ───────        │
│                            │
│  [📞 Call Us: (323) 471-]  │
│  [      6020             ] │
│  (click-to-call btn)       │
│                            │
└────────────────────────────┘
```

### Mobile Footer
```
┌────────────────────────────┐
│                            │
│  [LOGO]                    │
│                            │
│  The intelligence layer    │
│  behind profitable         │
│  insurance growth.         │
│                            │
│  [LinkedIn] [Twitter]      │
│                            │
│  ▼ Solutions               │
│  ▼ Industries              │
│  ▼ Resources               │
│  ▼ Company                 │
│  (accordion sections)      │
│                            │
│  ────────────────────      │
│                            │
│  GET IN TOUCH              │
│                            │
│  [📞 (323) 471-6020  ]    │
│  (click-to-call)           │
│                            │
│  1849 W 645 S              │
│  Orem, Utah 84059          │
│                            │
│  ────────────────────      │
│                            │
│  [email         ]          │
│  [ Subscribe    ]          │
│                            │
│  ────────────────────      │
│                            │
│  [SOC2] [TCPA] [Encrypt]   │
│                            │
│  © 2026 The Switchboard    │
│  Privacy · Terms           │
│                            │
└────────────────────────────┘
```

---

## Wireframe Notes

### Interaction Patterns
- **Mega menu**: Desktop only. Opens on hover with 200ms delay, closes on mouse leave
- **Mobile nav**: Full-screen overlay with accordion sub-menus
- **Multi-step form**: Progress bar at top, back/next buttons, step validation before advancing
- **Stats counters**: Animate counting from 0 when scrolled into viewport (IntersectionObserver)
- **Testimonial carousel**: Auto-advance every 6 seconds, pause on hover, swipe on mobile
- **FAQ accordion**: Single-open (opening one closes others), smooth height transition
- **Sticky elements**: Header becomes sticky with shadow after 100px scroll. Blog TOC sticky on desktop
- **Exit intent**: Triggers once per session when cursor moves above viewport (desktop only)

### Content Priority Per Page
| Page | Primary CTA | Secondary CTA | Tertiary |
|---|---|---|---|
| Homepage | Request Demo | See How It Works | View Case Studies |
| Solution | Request Demo | Read Case Study | Contact Sales |
| Industry | Request Demo | See [Industry] Case Study | Explore Solutions |
| About | Request Demo | Contact Us | View Careers |
| Contact | Submit Form | Book a Demo | Call Us |
| Blog Post | Request Demo (sidebar) | Subscribe Newsletter | Read Related |
| Case Study | Request Demo | Contact Sales | View More Studies |
| Resources | Download/Read Content | Subscribe Newsletter | Request Demo |
