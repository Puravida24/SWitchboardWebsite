# The Switchboard Website — Project Rules

## Workflow Rules (MANDATORY — NON-NEGOTIABLE)

### 1. TDD Red-Green-Refactor (RGR)
- **Always write a failing test FIRST** before writing any production code
- Make the test pass with minimal code (Green)
- Refactor while keeping tests green
- This applies to ALL code — new features AND modifications to existing code
- **No exceptions.** Never write production code without a failing test first.
- When modifying existing code, write a test that captures the new/changed behavior BEFORE changing the code.

### 2. Vertical Slicing with Demonstrable Deltas
- Every slice ships with admin UI so the user can test
- Each slice must produce a visible, testable delta
- **User confirms progress before moving to next slice** — never batch multiple slices silently
- See `VERTICAL_SLICE_PLAN.md` for the 8-slice delivery plan
- Complete one slice fully (tests + admin UI + public page) → present delta → wait for confirmation

### 3. Memento MCP Tools
- **Always prefer Memento MCP tools** over doing things manually whenever possible and relevant
- **Before ANY code work**: Reflect on all Memento rules, protocols, patterns, and best practices
- **After code work is complete**: Verify alignment with Memento rules and patterns
- Check CLAUDE.md (this file) and memory files before and after work

### 4. Pre/Post Code Work Checklist (MANDATORY)
**BEFORE starting any code task:**
- [ ] Re-read this CLAUDE.md file
- [ ] Check memory files for relevant context
- [ ] Review Memento patterns and learnings
- [ ] Confirm TDD RGR approach is planned
- [ ] Confirm which vertical slice this work belongs to

**AFTER completing any code task:**
- [ ] Verify TDD RGR was followed (failing test → pass → refactor)
- [ ] Verify no "lead/leads" in customer-facing copy
- [ ] Verify Memento tools were used where applicable
- [ ] Verify the delta is demonstrable and testable
- [ ] Capture any new knowledge/learnings to Memento

## Brand Rules
- **NEVER use "lead" or "leads"** in customer-facing copy
- Approved alternatives: prospects, opportunities, customers, shoppers, risk-scored opportunities
- Positioning: "Intelligence layer" — NOT "lead gen company"
- Design aesthetic: Apple-clean but unique, dark intelligence theme

## Tech Stack
- **Language**: C# 13 / .NET 9 / ASP.NET Core 9 with Razor Pages (SSR)
- **Database**: PostgreSQL 17 via EF Core 9 (Npgsql)
- **CSS**: Tailwind CSS 4 (CLI build pipeline)
- **JS**: Alpine.js for lightweight interactivity
- **Hosting**: Railway
- **Domain**: Namecheap (NOT Cloudflare)
- **Logging**: Serilog + Seq
- **Email**: MailKit via Amazon SES
- **CRM**: Phoenix CRM (DO NOT rebuild — webhook integration only)
- **Self-owned**: No third-party SaaS dependencies

## Project Structure
```
src/TheSwitchboard.Web/          — Main web application
src/TheSwitchboard.Web.Tests/    — Unit/integration tests
wireframes/                      — 10 design mockups (pending user selection)
```
