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

## Phoenix consent-match (TCPA dial-time verification)

Phoenix calls `POST /api/consent/match` before every dial to verify we have a
valid consent certificate for that prospect. Bearer-auth via
`PhoenixCrm:ConsentApiKey`. Response shape:

```
{ match: bool, matchedFields: [...], certificateExpired: false,
  consentTimestamp: ISO8601, disclosureVersion: { version, status } }
```

### Symptoms & fixes

**Phoenix reports "consent match failures spiking"**
1. `/Admin/Reports/Compliance` → check capture-rate panel. If it's cratered,
   a recent code change likely broke the cert-capture client (T-7B).
2. Seq: `@MessageTemplate like "%Phoenix consent match%" && match = false`.
   Inspect the first few fails — do the `matchedFields` list show empty?
   - Empty `matchedFields` → email/phone hashing mismatch (canonicalization
     drift between `consent-capture.js` and `ConsentMatchEndpoints.Sha256Hex`).
     Both must `Trim().ToLowerInvariant()` before hashing.
   - `certificateExpired = true` → `ExpiresAt` (CreatedAt + 5y) passed.
     Cannot retroactively extend; cert is dead. Record in an incident note.
3. `/Admin/Reports/Certificates/Index` → filter by DisclosureVersion. If
   recent submissions lack a cert at all, the client-side POST to
   `/api/tracking/consent` is failing. Check browser console on prod + Seq.

**Phoenix returns 401**
- Bearer token mismatch. Rotate `PhoenixCrm:ConsentApiKey` in Railway Vars,
  hand the new value to Phoenix ops, redeploy.
- Constant-time compare (`CryptographicOperations.FixedTimeEquals`) is used
  server-side — brute force via timing is not possible; stop looking there.

**Phoenix gets 410 Gone on a cert**
- Cert is past `ExpiresAt`. Phoenix must not dial on this lead. No server
  action needed; the 410 is correct.

**Phoenix gets 503**
- `PhoenixCrm:ConsentApiKey` env var not set. App refuses to answer until
  it's populated in Railway → Variables → redeploy.

---

## Retention + rollups (nightly 02:00 / 03:00 UTC)

`RollupService` aggregates raw events into `EventRollupDaily` at 02:00 UTC.
`DataRetentionJob` enforces retention limits at 03:00 UTC:

| Table                  | Retention     |
|------------------------|---------------|
| `ClickEvent`, `ScrollSample`, `MouseTrail`, `FormInteraction`, `WebVitalSample`, `BrowserSignal`, `AnalyticsEvent`, `PageView` | Delete after 90 d |
| `JsError`, `AlertLog`  | Delete after 1 y |
| `ReplayChunk.Payload`  | Soft-delete at 1 y (BYTEA nulled; `Replay` envelope row kept for session counts) |
| `ConsentCertificate`   | Governed by `ExpiresAt` (CreatedAt + 5 y) |
| everything else (Visitor, Session, Goal, DeployChange, DataSubjectRequest, EventRollupDaily, KnownProxyAsn, DisclosureVersion, AlertRule) | Never purged |

### "Where did last Tuesday's data go?"

If an admin chart shows a gap:

1. `/Admin/Reports/Health` → "Last rollup at X" panel. If X > 28h ago, the
   `RollupService` didn't run last night. Check Seq:
   `@MessageTemplate like "%Rollup%"` last 30 h.
2. Table may be purged — confirm it's inside the 90d window for raw events.
   Older gaps should be queried via `EventRollupDaily` instead, not raw.
3. If rollup hasn't caught up, manually invoke (admin-auth required):

```bash
curl -X POST 'https://www.theswitchboardmarketing.com/api/ops/rollup-backfill?fromDate=2026-04-10&toDate=2026-04-19' \
  -H "Authorization: Bearer $OPS_DEPLOY_TOKEN"
```

### Manual retention drill (quarterly)

Once a quarter, exercise the purge pipeline against a staging DB:

1. Seed 100 synthetic `ClickEvent` rows dated 95 d ago.
2. Trigger `DataRetentionJob.RunAsync` (via the admin-only ops endpoint or
   local debug entry).
3. Count should drop by 100. If any rows survive, the retention cutoff
   expression broke — file an incident.

---

## Alerts (admin)

`AlertEvaluatorService` polls every 5 min, evaluates `AlertRule` rows
against rolling windows, writes a row to `AlertLog` and dispatches via the
rule's `Channel` (email via SES, or webhook URL).

Seeded defaults (T-7C):
- Capture rate < 95 % per day
- New unregistered disclosure detected
- Bot rate > 5 % / hour

### When an alert fires

1. **Acknowledge first, investigate second.** `/Admin/Reports/Alerts` → set
   `AcknowledgedAt` + `AcknowledgedBy`. Prevents duplicate on-call pings.
2. Click into the alert detail — it links to the offending metric /
   disclosure / IP pattern with a full trace.
3. Common categories:
   - **Capture rate dipped** — likely a homepage deploy broke the form-
     capture client. `/Admin/Reports/Compliance` → check which submissions
     since the dip lack a cert, tie back to deploy commits via
     `/Admin/Reports/ChangesLog`.
   - **New unregistered disclosure** — someone edited the "By submitting..."
     paragraph. `/Admin/Reports/Disclosures` → compare text diff against the
     last Registered version. Register the new version (or roll the text
     back) to clear the alert.
   - **Bot rate elevated** — `/Admin/Reports/Visitors/Index` filter
     `IsBot=true` → look at `BotReason`. If it's one proxy ASN, optionally
     add to `KnownProxyAsn`; if it's a new bot pattern, add a rule to
     `IpClassificationService`.
4. **Snooze vs resolve** — `AcknowledgedAt` silences the same rule for 1 h;
   after that it re-fires if still in breach. Remove the rule or tighten
   the threshold if the fire is chronic.

### When the alert pipeline itself is broken

- No alerts have fired in 48h and you expect some → check Seq for
  `AlertEvaluatorService` errors. If the service is looping on an
  exception it logs but keeps going; the exception is the fix target.
- Email channel failing silently → check SES console → Sending activity
  for the alert From address. If SES is throttling, the rule logs the
  failure to `AlertLog.Context.deliveryError`.

---

## DSR (CCPA / GDPR delete request)

Legal escape valve — every personal-data table is wired into one service.

### Intake

1. Receive the deletion request (email to privacy@, Subject Access Request
   form, etc.). Capture: **email address**, **requester identity proof**
   (photo ID or auth'd login), **date received**.
2. Verify identity before acting. Refusing a forged DSR is protected by
   CCPA; complying with one is not.

### Execution

1. `/Admin/Reports/DSR` → "New Request" → paste email address.
2. Service runs `DataSubjectRequestService.ProcessAsync(email)`:
   - `FormSubmissions` rows — hard delete (email column matches)
   - `Session` rows where a cert linked to this email exists — hard delete
     plus cascade to `BrowserSignal`, `ClickEvent`, `ScrollSample`,
     `MouseTrail`, `FormInteraction`, `WebVitalSample`, `AnalyticsEvent`,
     `PageView`, `JsError`
   - `Replay` + `ReplayChunk` rows linked to those sessions — hard delete
   - `ConsentCertificate` rows (match on `EmailHash`) — **soft-erase PII
     fields** (EmailHash / PhoneHash / IpAddress nulled) while keeping the
     row for TCPA defense of any pre-DSR calls already placed
   - `Visitor` row — hard delete once all linked sessions are gone
3. Response on-screen shows per-table row counts. Screenshot for the DSR
   ticket; the counts are also persisted in `DataSubjectRequest.DeletedRowCounts`
   as JSON.
4. Status transitions: `pending` → `processing` → `complete`. Hard-fails
   (DB constraint, etc.) land in `denied` with an error note; on-call
   unblocks manually.

### Respond to the requester

Within 45 days per CCPA. Template:

> Hi [name],
> We've completed your data deletion request (reference #DSR-[id], received
> [date]). The following records linked to [email] have been removed:
> [paste counts from the admin page].
> Consent-certificate rows related to calls already placed have had all
> personal data fields erased; the record itself is retained for up to 5
> years as a TCPA defense, per our privacy policy.
> Reply to this email if you want anything further.

### If a DSR fails partially

`DataSubjectRequest.Status = "denied"` plus `DeletedRowCounts` will show
the partial counts. Do NOT retry with the same email hash — the second run
will find fewer rows and overwrite the first ticket's counts. File an
incident, fix the failing step, rerun against a fresh ticket.

---

**Last updated:** 2026-04-19 · Commit `95627d0`+ (A13 shipped)
