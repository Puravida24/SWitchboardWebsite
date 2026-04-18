# The Switchboard — Launch Checklist

After Slices 1–5 ship (all 5 shipped as of 2026-04-18), the automated build is complete. These items require external tools or human judgment and cannot be covered by xUnit.

## Pre-launch (block DNS cutover until all ✓)

### Performance (Core Web Vitals)
- [ ] **Lighthouse Performance ≥ 90** on `/` (mobile + desktop)
- [ ] **Lighthouse Performance ≥ 90** on `/privacy` `/terms` `/accessibility`
- [ ] **LCP < 2.5s** on 3G-throttled homepage (DevTools → Performance → Lighthouse)
- [ ] **INP < 200ms** on the contact form submit interaction
- [ ] **CLS < 0.1** on all public pages
- [ ] Critical CSS inlined in `<head>` · Non-critical JS deferred · Preconnect to fonts.gstatic.com
- [ ] Logo PNG served as WebP with `<picture>` fallback
- [ ] Image sizes in `<img>` to prevent layout shift

### Accessibility (WCAG 2.1 AA)
- [ ] **axe-core CLI run on `/` `/privacy` `/terms` `/accessibility` — 0 critical violations**
- [ ] Lighthouse Accessibility ≥ 95
- [ ] Keyboard-only navigation: tab order is logical through hero → nav → sections → contact → footer
- [ ] Skip-to-content link at top of `<main>` (SR users)
- [ ] All images have meaningful `alt` or `alt=""` if decorative
- [ ] Focus rings visible on all interactive elements
- [ ] Color contrast ≥ 4.5:1 on body text (`--warm-gray` + `--navy` on cream — verify)
- [ ] Form labels associated (`<label for>`) and error messages announced via `aria-live`
- [ ] Phoenix vignette has `aria-live="polite"` (already present)
- [ ] Partner carousel pauses on reduced-motion (respect `prefers-reduced-motion`)

### Cross-browser QA (manual)
- [ ] Chrome (latest desktop)
- [ ] Firefox (latest desktop)
- [ ] Safari 17+ (macOS)
- [ ] Edge (latest desktop)
- [ ] iOS Safari (iPhone 13+ real device, not simulator)
- [ ] Android Chrome (Pixel 6+ real device)
- [ ] For each: homepage renders, nav works, contact form submits, vignette plays, legal pages load

### Infrastructure
- [ ] Railway production deploy succeeds (auto-deploy from `main`)
- [ ] Production `/health` returns 200
- [ ] Production PostgreSQL: migrations applied cleanly (verify via `dotnet ef database update --connection <prod>`)
- [ ] Production `DATABASE_URL` + `DATABASE_PRIVATE_URL` set in Railway vars
- [ ] `Admin:Password` set in Railway vars (never commit the real one)
- [ ] `Analytics:IpHashSalt` set to a long random value (not the default)
- [ ] Seq URL configured (`Seq:ServerUrl`) and reachable
- [ ] Amazon SES domain verified + DKIM records added + from-address whitelisted
- [ ] Phoenix CRM webhook URL + secret set in Railway vars; test ping from `/Admin/Submissions/Detail`
- [ ] `Email:SmtpHost`, `SmtpPort`, `SmtpUsername`, `SmtpPassword`, `FromAddress`, `FromName` all set

### Content / brand
- [ ] Site settings filled in (admin → `/Admin/Settings`)
- [ ] Partners/roster populated with permission-cleared logos
- [ ] Legal pages reviewed by counsel (privacy, terms, accessibility)
- [ ] DMCA agent registered with U.S. Copyright Office OR clause removed
- [ ] Real email addresses created: `privacy@`, `legal@`, `dmca@`, `accessibility@`
- [ ] CI "no lead/leads" grep passes against all public pages

### SEO
- [ ] `/sitemap.xml` returns 200 with all public routes
- [ ] `/robots.txt` returns 200 and allows Googlebot
- [ ] `/llms.txt` returns 200
- [ ] Organization JSON-LD validates on [Rich Results Test](https://search.google.com/test/rich-results)
- [ ] Google Search Console property verified
- [ ] Submit sitemap to Search Console

## DNS cutover (Namecheap)

1. Lower TTL on existing records to 300s the day before
2. Confirm Railway custom domain target (`<project>.up.railway.app` or provided CNAME)
3. Namecheap → Advanced DNS:
   - [ ] `theswitchboardmarketing.com` → CNAME to Railway target
   - [ ] `www.theswitchboardmarketing.com` → CNAME to Railway target
   - [ ] Remove any old A records pointing at prior host
4. Wait for propagation (check `dig +short theswitchboardmarketing.com`)
5. Verify HTTPS certificate issued by Railway (Let's Encrypt)
6. Smoke test production: `/`, `/privacy`, `/terms`, `/accessibility`, `/admin/login`, `/health`, `/sitemap.xml`

## Post-launch (first 48 hours)

- [ ] Monitor Seq for ERROR+ log volume
- [ ] Monitor Phoenix CRM for webhook delivery failures
- [ ] Monitor SES bounces / complaints; suppression list empty
- [ ] Verify first real contact submission end-to-end (DB row + submitter email + team notification + Phoenix webhook)
- [ ] Check GA / Search Console for indexing signals
- [ ] Confirm visitor scorecard + easter eggs render (Chrome DevTools console)

## Rollback plan

- Railway one-click rollback to previous deployment from the Deployments tab
- If DB migration breaks: restore from Railway's daily Postgres backup (typically <24h RPO)
- Namecheap DNS can be reverted (but 300s TTL means a few minutes of cached lookups)

## Owner

- Build + deploy + code review: you
- Content proof + legal review: counsel
- Partner permissions: BD / founders
