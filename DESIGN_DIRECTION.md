# The Switchboard — Design Direction
**Date:** 2026-03-11
**Aesthetic:** Dark intelligence. Apple-clean. Not another B2B template.

---

## Design Philosophy

> "When someone lands on this site, they should feel like they just walked into a control room — calm, precise, powerful. Not a marketing brochure. A platform."

### Three Words
1. **Intelligent** — Every pixel communicates that this is a data company, not a lead vendor
2. **Confident** — No exclamation marks, no "revolutionize!", no startup energy. Quiet authority.
3. **Precise** — Tight spacing, deliberate typography, nothing wasted

### What We're NOT
- Not a generic B2B SaaS template (Framer/Webflow cookie-cutter)
- Not playful/colorful like Lemonade (we're B2B, not consumer)
- Not cluttered like MediaAlpha (our closest competitor looks dated)
- Not bento grid layout (overdone in 2025, feels like everyone else)

### Who We Channel
- **Scale AI** — dark bg, vibrant accents, 3D data visualization energy
- **Linear** — the gold standard of dark SaaS design, tight typography, subtle motion
- **Stripe** — editorial layouts, asymmetric grids, confidence in whitespace
- **Apple** — product-focused, let the thing speak for itself, restrained animation

---

## Color System

### Primary Palette — "Midnight Intelligence"
```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│   BACKGROUNDS                                               │
│                                                             │
│   ██████  navy-950  #030818   Hero backgrounds, depth       │
│   ██████  navy-900  #050b24   Primary dark sections         │
│   ██████  navy-800  #07102e   Card backgrounds on dark      │
│   ██████  navy-700  #091538   Hover states on dark          │
│   ██████  navy-600  #0b1a42   Secondary dark sections       │
│                                                             │
│   ACCENT — "Signal Blue"                                    │
│                                                             │
│   ██████  blue-500  #2563eb   Primary CTAs, active states   │
│   ██████  blue-400  #3b82f6   Hover on CTAs                 │
│   ██████  blue-300  #60a5fa   Links on dark backgrounds     │
│   ░░░░░░  blue-500/10         Glow effects behind CTAs      │
│   ░░░░░░  blue-500/5          Subtle card borders           │
│                                                             │
│   SEMANTIC                                                  │
│                                                             │
│   ██████  green-500 #059669   Success, positive metrics, ↑  │
│   ██████  amber-500 #d97706   Warnings, highlights          │
│   ██████  red-500   #ef4444   Errors, negative metrics, ↓   │
│                                                             │
│   NEUTRALS — "Moonlight"                                    │
│                                                             │
│   ██████  slate-50  #f8fafc   Light section backgrounds     │
│   ██████  slate-100 #f1f5f9   Card backgrounds on light     │
│   ██████  slate-300 #cbd5e1   Borders, dividers             │
│   ██████  slate-400 #94a3b8   Secondary text on dark        │
│   ██████  slate-500 #64748b   Body text on light            │
│   ██████  slate-900 #0f172a   Headings on light             │
│   ██████  white     #ffffff   Primary text on dark bg       │
│   ░░░░░░  white/60            Secondary text on dark bg     │
│   ░░░░░░  white/10            Subtle borders on dark bg     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Color Usage Rules
- **Dark sections** (hero, testimonials, CTAs): navy-950 or navy-900 bg, white text, blue-500 accents
- **Light sections** (features, content): slate-50 bg, slate-900 headings, slate-500 body
- **Alternating rhythm**: dark → light → dark → light (creates visual breathing)
- **Glow effects**: blue-500 with 10% opacity blur behind CTAs and interactive elements on dark bg
- **Gradients**: ONLY subtle (navy-950 → navy-900). No rainbow gradients. No loud gradients.
- **Borders on dark**: white/10 (barely visible, adds depth without clutter)

---

## Typography

### Font: Inter (self-hosted)
Why Inter: Apple uses SF Pro. Inter is the closest open-source equivalent. Clean, professional, excellent at small sizes and large displays. Already in the old site — brand continuity.

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│   DISPLAY                                                   │
│                                                             │
│   display-hero    Inter 800   64px/1.05  -0.03em            │
│                   (desktop)                                  │
│                   48px/1.1    -0.02em   (tablet)            │
│                   36px/1.15   -0.02em   (mobile)            │
│                                                             │
│   display-lg      Inter 700   48px/1.1   -0.02em            │
│   display-md      Inter 700   36px/1.15  -0.02em            │
│                                                             │
│   HEADINGS                                                  │
│                                                             │
│   heading-1       Inter 700   30px/1.2   -0.01em            │
│   heading-2       Inter 600   24px/1.25  -0.01em            │
│   heading-3       Inter 600   20px/1.3    0                 │
│   heading-4       Inter 600   18px/1.35   0                 │
│                                                             │
│   BODY                                                      │
│                                                             │
│   body-lg         Inter 400   18px/1.65   0                 │
│   body            Inter 400   16px/1.65   0                 │
│   body-sm         Inter 400   14px/1.55   0                 │
│   body-xs         Inter 500   12px/1.5    0                 │
│                                                             │
│   SPECIAL                                                   │
│                                                             │
│   overline        Inter 700   12px/1.5    0.08em  uppercase │
│   stat-number     Inter 800   56px/1.0   -0.03em            │
│   code            JetBrains   14px/1.6    0       monospace │
│                   Mono 400                                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Typography Rules
- **Headlines on dark bg**: white, Inter 700-800, tight letter-spacing (-0.02 to -0.03em)
- **Headlines on light bg**: slate-900
- **Body on dark bg**: white/80 for primary, white/60 for secondary
- **Body on light bg**: slate-500 for primary, slate-400 for secondary
- **Overlines**: ALWAYS uppercase, always the accent blue-500, always 12px, always tracking-wider
- **Line length**: Max 65 characters for body text (readability)
- **No italic anywhere** — not our voice. Bold or regular only.

---

## Layout System

### Grid
```
Max width: 1280px (xl breakpoint)
Columns: 12-column grid
Gutter: 32px (desktop), 24px (tablet), 16px (mobile)
Page padding: 64px (desktop), 32px (tablet), 20px (mobile)
```

### Section Spacing
```
Between major sections: 128px (desktop), 96px (tablet), 64px (mobile)
Within sections: 64px (desktop), 48px (tablet), 32px (mobile)
Between cards: 24px
```

### Layout Philosophy — "Editorial, Not Grid"
Instead of boring symmetric layouts, we use **asymmetric editorial layouts**:

```
INSTEAD OF THIS (boring):          DO THIS (editorial):
┌──────┐ ┌──────┐ ┌──────┐        ┌─────────────┐ ┌──────┐
│      │ │      │ │      │        │             │ │      │
│      │ │      │ │      │        │   LARGE     │ │SMALL │
│      │ │      │ │      │        │   FEATURE   │ │      │
└──────┘ └──────┘ └──────┘        │   CARD      │ ├──────┤
┌──────┐ ┌──────┐ ┌──────┐        │             │ │SMALL │
│      │ │      │ │      │        │             │ │      │
│      │ │      │ │      │        └─────────────┘ └──────┘
└──────┘ └──────┘ └──────┘
```

**Homepage uses a mixed layout rhythm:**
- Hero: full-width, asymmetric (text left, visual right)
- Social proof: full-width, centered
- Stats: 4-column, symmetric (exception — data should feel orderly)
- Capabilities: 2+3 asymmetric grid (2 large cards top, 3 smaller below)
- How it works: full-width, horizontal flow
- Industries: 3-column, symmetric (grouped equals)
- Testimonials: full-width, centered (single focus)
- Case studies: 2-column, asymmetric (one larger than the other)
- Final CTA: full-width, centered

---

## Component Design Language

### Buttons
```
PRIMARY (on dark bg):
┌─────────────────────────────────┐
│                                 │
│   ████████████████████████████  │  bg: blue-500
│   ██  Request a Demo         ██  │  text: white
│   ████████████████████████████  │  padding: 14px 28px
│                                 │  radius: 10px
│   Hover: blue-400 + subtle      │  font: Inter 600 16px
│   glow (box-shadow: 0 0 30px    │  transition: all 200ms
│   blue-500/25)                  │
│                                 │
└─────────────────────────────────┘

SECONDARY (on dark bg):
┌─────────────────────────────────┐
│                                 │
│   ┌────────────────────────┐    │  bg: transparent
│   │  See How It Works   →  │    │  border: 1px white/20
│   └────────────────────────┘    │  text: white
│                                 │  Hover: bg white/5
│   Ghost style with subtle       │  border white/40
│   border                        │
│                                 │
└─────────────────────────────────┘

PRIMARY (on light bg):
┌─────────────────────────────────┐
│                                 │
│   ████████████████████████████  │  bg: navy-900
│   ██  Get Started            ██  │  text: white
│   ████████████████████████████  │  Hover: navy-800
│                                 │
└─────────────────────────────────┘
```

### Cards
```
FEATURE CARD (on light bg):
┌─────────────────────────────────┐
│                                 │
│  ┌───────────────────────────┐  │
│  │                           │  │  bg: white
│  │  ┌────┐                   │  │  border: 1px slate-200
│  │  │icon│  ← blue-500 icon  │  │  radius: 16px
│  │  └────┘                   │  │  padding: 32px
│  │                           │  │  shadow: 0 1px 3px black/5
│  │  Data Enrichment          │  │
│  │  ─────────────────        │  │  Hover: shadow grows,
│  │                           │  │  border blue-500/20,
│  │  Enhance prospect data    │  │  translate-y -2px
│  │  with 90GB+ of            │  │
│  │  proprietary intelligence │  │
│  │                           │  │
│  │  Learn More →             │  │  Link: blue-500
│  │                           │  │
│  └───────────────────────────┘  │
│                                 │
└─────────────────────────────────┘

FEATURE CARD (on dark bg):
┌─────────────────────────────────┐
│                                 │
│  ┌───────────────────────────┐  │
│  │                           │  │  bg: navy-800
│  │  ┌────┐                   │  │  border: 1px white/10
│  │  │icon│  ← blue-400 icon  │  │  radius: 16px
│  │  └────┘                   │  │  padding: 32px
│  │                           │  │
│  │  Data Enrichment          │  │  Hover: border white/20,
│  │  ─────────────────        │  │  bg navy-700,
│  │                           │  │  subtle blue glow
│  │  Enhance prospect data    │  │
│  │  with 90GB+ of            │  │  Text: white (title),
│  │  proprietary intelligence │  │  white/60 (body)
│  │                           │  │
│  │  Learn More →             │  │  Link: blue-400
│  │                           │  │
│  └───────────────────────────┘  │
│                                 │
└─────────────────────────────────┘

STAT CARD:
┌─────────────────────────────────┐
│                                 │
│  ┌───────────────────────────┐  │
│  │                           │  │  bg: transparent or
│  │         10M+              │  │  navy-800/50
│  │    ─────────────          │  │
│  │    Prospects              │  │  Number: stat-number
│  │    Processed              │  │  (Inter 800, 56px, white)
│  │                           │  │
│  │                           │  │  Label: body-sm
│  └───────────────────────────┘  │  (white/60)
│                                 │
│  Divider between stats:         │  Vertical line: white/10
│  thin vertical line             │
│                                 │
└─────────────────────────────────┘
```

### Navigation
```
HEADER (desktop):
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  bg: navy-950 (transparent at top, solid on scroll)                  │
│  height: 72px                                                        │
│  border-bottom: 1px white/5 (only when scrolled)                     │
│  backdrop-filter: blur(12px) when scrolled (frosted glass)           │
│                                                                      │
│  [LOGO]        Solutions  Industries  Resources  About  Contact      │
│                                                                      │
│  Nav text: white/70                                                  │
│  Nav hover: white                                                    │
│  Active: white + blue-500 underline (2px, offset 4px)                │
│                                                                      │
│                                              ┌─────────────────┐     │
│                                              │ Request a Demo  │     │
│                                              └─────────────────┘     │
│                                              (blue-500 bg, sm size)  │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘

MEGA MENU:
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  bg: navy-900                                                        │
│  border: 1px white/10                                                │
│  border-radius: 16px (detached from header, floating)                │
│  shadow: 0 25px 50px black/40                                        │
│  margin-top: 8px (gap between header and menu)                       │
│  max-width: 720px                                                    │
│  backdrop-filter: blur(20px)                                         │
│                                                                      │
│  Items have hover bg: white/5                                        │
│  Icons: blue-400, 20px                                               │
│  Title: white, Inter 600                                             │
│  Description: white/50, Inter 400, 14px                              │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Hero Section — The Money Shot

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  bg: navy-950                                                        │
│  height: calc(100vh - 72px) or min 700px                             │
│                                                                      │
│  ┌──── LEFT (text) ──────────────────┐  ┌──── RIGHT (visual) ────┐  │
│  │                                    │  │                        │  │
│  │  INSURANCE INTELLIGENCE PLATFORM   │  │  ┌──────────────────┐ │  │
│  │  ↑ overline, blue-500, 12px        │  │  │                  │ │  │
│  │                                    │  │  │  INTERACTIVE     │ │  │
│  │  The Intelligence Layer            │  │  │  DATA            │ │  │
│  │  Behind Profitable                 │  │  │  VISUALIZATION   │ │  │
│  │  Insurance Growth                  │  │  │                  │ │  │
│  │  ↑ display-hero, white, 64px       │  │  │  Animated        │ │  │
│  │                                    │  │  │  particles/nodes │ │  │
│  │  Transform raw prospect data       │  │  │  connecting to   │ │  │
│  │  into risk-scored, conversion-     │  │  │  represent data  │ │  │
│  │  predicted opportunities —         │  │  │  flowing through │ │  │
│  │  delivered in real-time to the     │  │  │  the platform.   │ │  │
│  │  right buyer at the right price.   │  │  │                  │ │  │
│  │  ↑ body-lg, white/70, 18px        │  │  │  Subtle blue     │ │  │
│  │                                    │  │  │  glow on nodes.  │ │  │
│  │  ┌─────────────────┐ ┌─────────┐  │  │  │  Slow, ambient.  │ │  │
│  │  │ Request a Demo  │ │ See How │  │  │  │  NOT flashy.     │ │  │
│  │  │  (blue-500 bg)  │ │It Works │  │  │  │                  │ │  │
│  │  └─────────────────┘ └─────────┘  │  │  └──────────────────┘ │  │
│  │  ↑ primary + ghost buttons        │  │                        │  │
│  │                                    │  │  CSS/canvas animation  │  │
│  │  Trusted by 50+ insurance          │  │  or static SVG with    │  │
│  │  agencies processing 10,000+       │  │  CSS animation for     │  │
│  │  prospects daily                   │  │  performance           │  │
│  │  ↑ body-sm, white/40              │  │                        │  │
│  │                                    │  │                        │  │
│  └────────────────────────────────────┘  └────────────────────────┘  │
│                                                                      │
│  BACKGROUND EFFECT:                                                  │
│  Subtle radial gradient from navy-900 center to navy-950 edges       │
│  Optional: very faint dot grid pattern (white/3) for texture         │
│  Blue-500/5 gradient blob in top-right (behind the visual)           │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Hero Visual Options (pick one):

**Option A: Animated Network Graph** (recommended)
- Nodes represent data points (prospects, risk scores, carriers)
- Lines connect nodes showing data flow
- Nodes pulse with blue-500 glow
- Slow, ambient animation (not distracting)
- Built with CSS animations on SVG (no heavy JS library)
- Feels like: "You're looking at a living intelligence system"

**Option B: Abstract Data Mesh**
- Flowing geometric mesh/grid that subtly shifts
- Blue-500 accent lines on navy background
- Gradient overlay creates depth
- CSS animation only
- Feels like: "Data in motion"

**Option C: Dashboard Mockup**
- Stylized screenshot of the actual Switchboard dashboard
- Floating at an angle with perspective transform
- Subtle shadow and glow behind it
- Shows real UI (risk scores, charts, metrics)
- Feels like: "This is a real product with a real interface"

**Option D: Minimal Abstract**
- Single geometric element (circle, hexagon) with internal animation
- Think: Apple product reveal energy
- Maximum whitespace, let typography carry the weight
- Feels like: "We're so confident we don't need to try hard"

---

## Section-by-Section Design

### Social Proof Bar
```
bg: navy-900 (or navy-950 to blend with hero)
Thin top border: white/5

"Trusted by leading insurance organizations"
↑ body-sm, white/30, centered

Logos: white/40 (desaturated/monochrome), hover → white/80
Continuous scroll animation (CSS marquee, not JS)
Height: 80px total
```

### Stats Counter Section
```
bg: transparent (sits on navy-950, continuous with hero)
OR: slate-50 if transitioning to light section

Stats in a row with thin white/10 vertical dividers between them
Numbers animate UP on scroll (Alpine.js + IntersectionObserver)

   10M+          <250ms         340%          50+
   Prospects     Enrichment     Avg ROI       Agency
   Processed     Time           Lift          Partners

Number: Inter 800, 56px, white (or navy-900 on light bg)
Label: Inter 400, 14px, white/50 (or slate-500 on light bg)

NO card backgrounds. Clean, floating numbers.
```

### Platform Capabilities
```
bg: slate-50 (light section — breathing room)

Layout: 2 large cards on top row, 3 cards on bottom row
(NOT a symmetric 3x2 — asymmetric is more interesting)

┌─────────────────────────┐  ┌─────────────────────────┐
│                         │  │                         │
│  Data Enrichment        │  │  Risk Segmentation      │
│  (larger card, more     │  │  (larger card, more     │
│   detail, feature       │  │   detail)               │
│   bullets)              │  │                         │
│                         │  │                         │
└─────────────────────────┘  └─────────────────────────┘

┌───────────────┐  ┌───────────────┐  ┌───────────────┐
│ Quality       │  │ Performance   │  │ Decision      │
│ Scoring       │  │ Analytics     │  │ Engine        │
└───────────────┘  └───────────────┘  └───────────────┘

Cards: white bg, 1px slate-200 border, 16px radius, 32px padding
Hover: lift -2px, shadow grows, border → blue-500/20
Icons: blue-500, 32px (in a 48px circle with blue-500/10 bg)
```

### How It Works
```
bg: navy-950 (dark section)

Horizontal flow with connecting lines (not arrows — too generic)
Lines are blue-500/30, animated dash pattern flowing left-to-right

   ┌──────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐
   │          │ ···> │          │ ···> │          │ ···> │          │
   │  Intake  │      │  Enrich  │      │  Score   │      │ Deliver  │
   │          │      │& Segment │      │ & Route  │      │          │
   └──────────┘      └──────────┘      └──────────┘      └──────────┘

Step boxes: navy-800 bg, white/10 border, 12px radius
Step number: blue-500 circle with white number
Step title: white, Inter 600
Step description: white/50, Inter 400

Center badge: "< 250ms end-to-end" in a pill badge (blue-500/10 bg, blue-400 text)
```

### Testimonials
```
bg: navy-900

Single testimonial, centered, large quote, maximum impact
NO carousel dots visible by default (clean)
Subtle fade transition between testimonials (Alpine.js)

"The Switchboard transformed our customer acquisition.
 We went from buying blind to receiving risk-scored,
 conversion-predicted opportunities. Our close rate
 increased 47% in the first quarter."

↑ Inter 400, 24px, white, italic-EXCEPTION (quotes can be italic)
  Line-height 1.6, max-width 720px, centered

Photo: 56px circle, 2px blue-500 border
Name: white, Inter 600, 16px
Title + Company: white/50, Inter 400, 14px

Navigation: small arrows at edges, very subtle (white/20, hover white/60)
Auto-advance indicator: thin progress bar below (blue-500, fills over 6s)
```

### Case Study Preview
```
bg: slate-50 (light section)

Two cards, slightly asymmetric (left card: 55% width, right: 42%)

Card design:
- White bg, slate-200 border, 16px radius
- Client logo at top (desaturated)
- Key metrics in a blue-500/5 bg strip:
  "+340% ROI lift   |   47% close rate"
  ↑ metrics in green-500 (positive) or blue-500
- Title: slate-900, Inter 600
- Meta: slate-400, Inter 400
- "Read Case Study →" link: blue-500

Hover: card lifts, shadow grows, subtle border shift
```

### Final CTA
```
bg: navy-950
Centered layout, generous padding (128px top/bottom)

Headline: display-lg, white
"Ready to Make Every Prospect Count?"

Subtext: body-lg, white/60, max-width 540px
"See how The Switchboard's risk segmentation
intelligence can transform your acquisition economics."

Two buttons centered:
[Request a Demo] — white bg, navy-900 text (inverted primary)
[Contact Sales] — ghost white (1px white/20 border)

Trust line below: ✓ No commitment  ✓ 30-min overview  ✓ Custom ROI analysis
↑ body-sm, white/40, checkmarks in green-500
```

---

## Interaction & Animation Guidelines

### Philosophy: "Felt, Not Seen"
Animations should feel natural and responsive, never decorative or attention-seeking. If someone notices the animation, it's too much.

### Allowed Animations
```
TRANSITIONS (all 200ms ease-out):
- Button hover: bg color shift + subtle glow
- Card hover: translate-y -2px + shadow grow
- Nav link hover: opacity 0.7 → 1.0
- Mega menu: opacity + translate-y 8px → 0 (fade down in)
- Mobile menu: slide in from right (300ms)
- FAQ accordion: height transition (300ms ease)

SCROLL-TRIGGERED (once per page load):
- Stats counter: count up from 0 (2s duration, ease-out)
- Section fade-in: opacity 0 → 1 + translate-y 20px → 0 (subtle entrance)
- Staggered cards: each card fades in 100ms after previous

CONTINUOUS:
- Logo carousel: constant horizontal scroll (CSS animation, 30s loop)
- Hero visual: slow ambient animation (particles/mesh, 60s+ cycle)
- Testimonial progress bar: fills over 6 seconds
- Process flow lines: animated dashes flowing left-to-right

NOT ALLOWED:
- Parallax scrolling (feels cheap in 2026)
- Bounce/elastic animations (too playful)
- Page transition animations (slow, annoying)
- Cursor-following effects (gimmicky)
- 3D perspective tilts on hover (overdone)
- Confetti or particle explosions
- Typewriter text effects
```

### Loading States
```
Page load: Instant SSR (Razor Pages), no skeleton screens needed
Image lazy load: blur-up placeholder (10px blurred version → full image)
Form submit: Button shows spinner, disables during submission
Chat widget: Typing indicator (3 animated dots)
```

---

## Mobile Design (< 768px)

### Key Differences
- Hero: stacked (text above, visual below), full-width buttons
- Stats: 2x2 grid (not 4-column)
- Cards: single column, full-width
- Navigation: full-screen overlay from right
- Footer: accordion sections
- Font sizes reduce by ~20% (display-hero: 36px instead of 64px)
- Section spacing: 64px instead of 128px
- Touch targets: minimum 44x44px
- Click-to-call button on contact page

### Mobile-Specific Patterns
```
- Sticky "Request a Demo" button at bottom of screen (after scrolling past hero)
  40px height, blue-500 bg, slides up on scroll down, slides away on scroll up

- Swipe for carousels (testimonials, case studies)

- Phone number is always a tel: link

- Hamburger animates to X on open (CSS transition)
```

---

## Dark Mode Note

The site IS already "dark mode" by default for the primary brand experience. We do NOT offer a light/dark toggle. The dark navy IS our brand. Light sections (slate-50) provide contrast and breathing room — they're part of the design rhythm, not an alternative mode.

---

## Inspiration Board Reference

| Site | What to Study | URL |
|---|---|---|
| **Scale AI** | Hero energy, dark bg, trust bar, data viz | scale.com |
| **Linear** | Typography, micro-interactions, dark cards | linear.app |
| **Stripe** | Editorial layouts, asymmetric grids, confidence | stripe.com |
| **Vercel** | Dark nav, frosted glass header, SSR speed | vercel.com |
| **Pipe** | Fintech dark theme, green accents on dark | pipe.com |
| **Raycast** | Product screenshots floating, smooth scroll | raycast.com |

---

## What Makes This Uniquely "Switchboard"

1. **The navy-950 depth** — Not just dark mode. This is DEEP midnight. Competitors use gray or lighter blues. We go darker.
2. **The blue-500 glow** — Subtle emanating glow on interactive elements creates a "signal" metaphor. Data signals. Intelligence signals.
3. **The network visual** — Hero animation of connected nodes represents the literal switchboard concept — routing intelligence between points.
4. **The precision** — Tight letter-spacing, deliberate spacing, nothing decorative. This is how an engineering-driven company presents itself.
5. **The metrics-forward approach** — Numbers are always the biggest thing on the page. Not words. Numbers. Because we're a data company.

---

*This is not a template. This is The Switchboard.*
