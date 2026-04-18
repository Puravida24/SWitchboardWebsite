# The Switchboard — Backup & Restore Procedure

How to recover the production Postgres database when something bad happens.

## Recovery targets

| Metric | Target | Reality |
|---|---|---|
| **RPO (Recovery Point Objective)** — max acceptable data loss | 24 hours at launch; drop to 1 hour once traffic >10 submissions/day | Railway's daily snapshot covers RPO=24h automatically |
| **RTO (Recovery Time Objective)** — max acceptable downtime during restore | 1 hour | Railway restore takes ~10–30 min in practice |

We tolerate losing up to 24 hours of contact-form submissions in a catastrophic-failure scenario because Phoenix CRM has a parallel copy of every record that fires through the webhook. The DB is a secondary source of truth, not the only one.

## Backup sources

### 1. Railway-managed daily Postgres snapshots (primary)

Railway automatically snapshots the managed Postgres service once per day. Snapshots are stored on Railway's infrastructure and retained per their policy (currently 7 days rolling).

- **No action required to create them.** Backups run automatically.
- **Visibility:** Railway dashboard → `believable-forgiveness` project → Postgres service → "Backups" tab.

### 2. Phoenix CRM mirror (secondary)

Every contact-form submission also fires a webhook to Phoenix CRM. Phoenix retains its own copy. In a catastrophic DB loss, 90%+ of the customer-facing records can be reconstructed from Phoenix by export + replay.

### 3. Git (tertiary)

Every migration lives under `src/TheSwitchboard.Web/Migrations/`. If we need to reconstruct the schema from scratch on a new Postgres, `dotnet ef database update` replays all migrations to current state.

---

## Restore procedure — Railway snapshot

### Scenario: "a migration broke prod and we need yesterday's DB back"

1. **Go to the Railway Postgres service → Backups tab.**
2. Identify the snapshot timestamp you want to restore to.
3. Click **"Restore"** on that row. Railway will prompt for confirmation.
4. Railway spins up a new Postgres instance seeded from the snapshot. Takes 5–10 min for DBs under 1 GB.
5. Once the new instance is Running, **update the app's `DATABASE_URL` env var** to point at the new Postgres service.
6. Redeploy the app (Railway auto-redeploys on var change).
7. Verify at `/health` — must return 200.
8. Spot-check `/Admin/Submissions` — row count should match the restore-point's row count.

### After restore

- **Phoenix reconciliation:** export the recent Phoenix records (Phoenix admin UI) that post-date the restore point. For each, verify the corresponding DB row exists; if not, manually re-insert from the Phoenix export.
- **Seq correlation:** cross-reference Seq logs in the gap between restore-point and restore-completion to identify any submissions that happened during the outage. Those are the ones most at risk of being lost.
- **Incident write-up:** drop a note in `LAUNCH_CHECKLIST.md` or a new `INCIDENTS.md` with: what broke, when, what we restored to, what was lost.

---

## Restore procedure — full recovery from scratch (worst case)

If Railway itself goes sideways and we lose both the DB and the snapshots:

1. Provision a new Postgres anywhere (Railway, Render, AWS RDS).
2. Update `DATABASE_URL` in Railway to point at the new Postgres.
3. On next deploy, the app's boot-time `db.Database.MigrateAsync()` call will replay all EF migrations to create the schema.
4. The seeded admin user + default SiteSettings rows are re-inserted by `AdminSeedService` on first boot.
5. Contact-form history is **lost** from our DB. Export from Phoenix if needed.

---

## Testing the restore procedure

**This must be tested at least once before first real incident.** Put it on the launch checklist.

Test procedure:
1. Create a labeled submission in production: name = "Backup Test {today's date}"
2. Wait 24 hours for a snapshot to include it.
3. In a staging Railway project, restore that snapshot.
4. Verify the labeled row exists in the staging DB.
5. Tear down the staging Postgres to avoid cost.

Log the test result here with date + outcome:

- `2026-MM-DD` — _untested_

---

## Secrets backup

Env vars in Railway are NOT part of the Postgres snapshot. If the Railway account itself is lost:

- **Admin:Password / Analytics:IpHashSalt / Ses:WebhookSecret / PhoenixCrm:ApiKey** — regenerable. Rotate them and update the env vars on the new deployment.
- **SES SMTP credentials** — regenerable in AWS IAM console.
- **Phoenix webhook URL/key** — coordinate with Phoenix team for a reissue.

Do not store these values in the code repo or in this file.

---

**Last updated:** 2026-04-18 · H-7 shipped
