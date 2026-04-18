# The Switchboard — Incident Runbook

Operational playbook for the production website at `https://www.theswitchboardmarketing.com`.
Written for on-call response. Keep this file terse and actionable. Long-form
architecture docs live elsewhere.

## Ownership

- **Production code & infra:** Ryan Justin
- **Content (site copy, legal pages):** Ryan + counsel
- **Partner logo permissions:** BD team
- **SES domain + DKIM:** Ryan

## Quick reference

| Surface | Where |
|---|---|
| Production URL | `https://www.theswitchboardmarketing.com` |
| Railway project | `believable-forgiveness` |
| DB | Railway-managed Postgres 17 |
| Email | Amazon SES |
| CRM webhook | Phoenix (external) |
| Log aggregation | Seq (self-hosted) |
| Error paging | _not yet wired — see "Outstanding work"_ |

---

## When something is on fire

### 1. Quick triage (30 seconds)

```bash
curl -sI https://www.theswitchboardmarketing.com/health | head -1
```

- **`HTTP/2 200`** → app is responding. Problem is likely partial (a specific endpoint, a specific integration). Skip to the relevant section below.
- **`HTTP/2 5xx`** → app is up but erroring. Check Seq immediately (see "Logs").
- **Connection refused / timeout** → app is down. See "App is down".

### 2. Correlate with latest deploy

```bash
git log -5 --pretty=format:"%h %ci %s"
```

If incident started around the last deploy time, that commit is your prime suspect. Roll back via Railway UI → Deployments tab → one-click revert to prior deploy.

### 3. Check Seq for errors

Seq URL is in the `Seq:ServerUrl` Railway env var. Filter by:
- `@Level = "Error"` in the last 15 minutes
- `CorrelationId` if a user gave you one (added H-7; every request tags its logs)

---

## Rollback — safest reversion paths

**Code rollback (deploy-level):**
1. Railway → Deployments tab → find the previous successful deploy
2. Click "Redeploy" on it
3. ~90 sec later: `curl /health` returns 200 on the old code
4. Post-mortem the bad commit before re-merging

**DNS rollback (in case of a bad infrastructure change):**
- Squarespace Domains → DNS settings
- Revert the last change (A @ or CNAME www)
- Propagation ~5 min due to our TTL of 300 s
- `dig +short www.theswitchboardmarketing.com` to verify

**DB rollback:** see `BACKUP_RESTORE.md` — we restore from a Railway point-in-time snapshot, not by replaying migrations.

---

## Rotate admin password (compromise or scheduled rotation)

1. Railway dashboard → `believable-forgiveness` → Variables
2. Edit `Admin:Password` → paste a fresh 16+ char random string
3. Redeploy (Railway triggers automatically on var change)
4. The `AdminSeedService` updates the seeded admin record on boot with the new password hash (Argon2id).
5. Confirm the NEW password works at `https://www.theswitchboardmarketing.com/Admin/Login`
6. **If the old password was leaked** — also rotate:
   - `Analytics:IpHashSalt` (so logs from pre-compromise don't match future lookups)
   - `Ses:WebhookSecret`
   - `PhoenixCrm:ApiKey`

---

## App is down

1. Railway → Deployments → check if the latest deploy failed. If yes, redeploy the prior one (rollback).
2. Railway → Metrics → check CPU / RAM / restart count. If the container is in a restart loop, the root cause is in the app boot — check Seq for startup errors.
3. If DB is unreachable (health check in `AppDbContext` fails at boot), the app will fail fast. Check the `DATABASE_URL` env var is still set and pointing at a healthy Postgres.
4. If everything looks fine on Railway but DNS is broken, see "DNS rollback" above.

---

## Logs (Seq)

- **URL:** value of Railway env var `Seq:ServerUrl`
- **Correlation by request:** filter `CorrelationId = "<value>"` — every log line since H-7 tags the ID (from the response `X-Correlation-ID` header)
- **PII redaction:** `PiiRedactionEnricher` masks Email / Phone / IpAddress before logs are written (shipped in Slice 4)
- **Common queries:**
  - All errors last 15 min: `@Level = "Error" && @Timestamp > Now() - 15m`
  - Phoenix webhook failures: `@MessageTemplate like "%Phoenix webhook failed%"`
  - Rate-limited calls: `@MessageTemplate like "%Rate limit exceeded%"`

---

## Common integration failures

### SES (outbound email) bounces or rejects

- **SES SMTP credentials invalid** → `Email:SmtpUsername` / `Email:SmtpPassword` env vars
- **Domain not verified** → check SES console → Verified Identities → `theswitchboardmarketing.com` should be "Verified"
- **DKIM records missing** → Squarespace DNS must have the 3 CNAME records SES gave us
- **In SES sandbox mode** → can only send to verified recipient addresses. Request production access in AWS console.

### Phoenix CRM webhook failures

- Phoenix rejects our payload → check `PhoenixCrm:WebhookUrl` is correct and the `PhoenixCrm:ApiKey` header is being sent
- Check the submission in `/Admin/Submissions` — `PhoenixSyncStatus` shows `Pending` / `Sent` / `Failed` / `DeadLettered` (after 3 failed attempts, shipped in H-4)
- Manual retry: from admin, call the retry endpoint (documented in API)

### Contact form submissions silently dropping

- Honeypot field triggered (bot protection) — expected behavior, logged as "Contact submission rejected: honeypot"
- Submit-timing guard (less than 2s between page load and submit) — same behavior
- Origin check rejecting cross-origin POSTs — returns 403, logged

---

## Env var checklist for a new Railway environment

```
Admin:Password                          (required — long random string)
Analytics:IpHashSalt                    (required — long random string)
DATABASE_URL                            (auto-set by Railway Postgres)
Email:SmtpHost                          email-smtp.us-east-1.amazonaws.com (for SES)
Email:SmtpPort                          587
Email:SmtpUsername                      (from SES SMTP credentials)
Email:SmtpPassword                      (from SES SMTP credentials)
Email:FromAddress                       noreply@theswitchboardmarketing.com
Email:FromName                          The Switchboard
Email:InternalNotificationAddress       comma-separated list of internal recipients
PhoenixCrm:WebhookUrl                   (Phoenix webhook endpoint)
PhoenixCrm:ApiKey                       (Phoenix-issued API key)
Ses:WebhookSecret                       (long random; signs inbound /api/ses/bounce webhook)
Seq:ServerUrl                           https://seq.theswitchboardmarketing.com
```

---

## Outstanding ops work

- No PagerDuty / OpsGenie integration yet — errors surface in Seq; on-call must check proactively.
- No load-test baseline — traffic assumption is <5 rps at launch; re-baseline at 10 rps and 100 rps.
- DMCA agent not registered (site hosts no UGC — not needed unless that changes).
- Apex domain (`theswitchboardmarketing.com` without www) still hits Squarespace's default parking — acceptable by decision; www is canonical.

---

**Last updated:** 2026-04-18 · Commit `0438abf`+ (H-7 shipped)
