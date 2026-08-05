# Deployment Guide

Operational reference for running CodeCraft.NET outside local dev: required
configuration, TLS, migrations, backups, and CI.

## 1. Environment configuration

Copy `.env.example` to `.env` and fill in the required values:

| Variable | Required in prod | Purpose |
| --- | --- | --- |
| `DB_PASSWORD` | Yes | Postgres password. `openssl rand -base64 24` |
| `JWT_SECRET` | Yes | JWT signing key, 32+ chars. `openssl rand -base64 48` |
| `CORS_ORIGIN` | Yes | Public frontend origin, e.g. `https://codecraftnet.example.com`. The api refuses to start in `Production` without this — see `Program.cs`. |
| `ADMIN_EMAIL` | No (has default) | Account(s) that get the Admin claim. |
| `DB_PORT` / `API_PORT` / `WEB_PORT` | No | Host port overrides. |
| `OLLAMA_*` | No | Optional local AI feedback/question generation. |

Local dev (`docker compose up`) works with none of this set — every value
falls back to a dev-safe default baked into `docker-compose.yml`.

## 2. Running in production

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

`docker-compose.prod.yml` layers on top of the base file and:

- Sets `ASPNETCORE_ENVIRONMENT=Production` (disables Swagger, enables the CORS allow-list check, enables `UseHttpsRedirection`).
- Requires `DB_PASSWORD`, `JWT_SECRET`, `CORS_ORIGIN` — compose refuses to start without them.
- Stops publishing `db` and `api` directly to the host; only `web` (nginx) is internet-facing. The api is reached exclusively through nginx's `/api/` reverse proxy.
- Applies memory/CPU limits to `db`, `api`, `web` (the `runner` sandbox already had limits — see `docker-compose.yml`).
- Swaps in `docker/nginx.prod.conf`: HTTP→HTTPS redirect, HSTS + security headers, TLS termination.

## 3. TLS certificates

`docker-compose.prod.yml` mounts `./docker/certs` read-only into the `web`
container at `/etc/nginx/certs`, expecting `fullchain.pem` and `privkey.pem`.

**Real domain (Let's Encrypt):** run certbot in webroot or standalone mode on
the host, then copy/symlink the issued cert pair into `docker/certs/`. Renewals
need a `docker compose restart web` (or reload) afterwards.

**Local/staging smoke test (self-signed):**

```bash
mkdir -p docker/certs
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout docker/certs/privkey.pem -out docker/certs/fullchain.pem \
  -subj "/CN=localhost"
```

Browsers will warn on the self-signed cert; that's expected for testing only.

## 4. Database migrations

The api applies EF Core migrations automatically on startup
(`CodeCraftNetSeeder.SeedAsync` → `dbContext.Database.MigrateAsync`) when
running against Postgres. No manual migration step is needed for:

- A fresh production database — `MigrateAsync` builds the full schema from
  `src/CodeCraftNet.Infrastructure/Persistence/Migrations`.
- Any Postgres database that was already created through migrations.

The two SQL files under `docker/` (`baseline-migrations.sql`,
`schema-delta-onboarding.sql`) are **one-time, historical fixups** for
databases created before this project adopted EF migrations (i.e. via
`EnsureCreated`, back when the schema was managed by hand). They are
idempotent and safe to re-run, but a new production deployment will never
need them — only run them if you're promoting a pre-migration
dev/staging database. See the comments at the top of each file for the exact
command.

Adding a new migration going forward: `dotnet ef migrations add <Name>
--project src/CodeCraftNet.Infrastructure --startup-project
src/CodeCraftNet.Api`. It ships in the next image build/deploy and applies
automatically on the next api startup.

## 5. Backups

```bash
./scripts/backup-db.sh              # writes ./backups/codecraftnet_db_<timestamp>.sql.gz
./scripts/restore-db.sh <file.sql.gz>  # destructive: drops + recreates the DB first
```

Schedule `backup-db.sh` with cron (see the comment at the top of the script
for a daily example). Store backups off-host (object storage, another
machine) — a backup that lives only on the same disk as the database doesn't
protect against disk/host loss.

## 6. Health checks

- `GET /health/live` — process is up, no dependency checks. Used by the
  `api` container's Docker healthcheck.
- `GET /health/ready` — also confirms the database is reachable. Wire this
  into any external uptime/orchestration check that should reflect real
  readiness, not just "the process didn't crash."
- `GET /api/health` — pre-existing lightweight endpoint the frontend's
  connection badge polls; unchanged.

## 7. CI

`.github/workflows/ci.yml` runs on every push/PR to `main`: restores,
builds, runs unit + integration tests, and builds the `api`/`runner` Docker
images (build-only — nothing is pushed). Wire in a push-to-registry step
(GHCR, ECR, etc.) once you have somewhere to deploy the built images to.

## 8. Known gaps / follow-ups

- **Frontend build**: `app/` is still served as raw JSX + CDN React/Babel/
  Tailwind (no bundling/minification). This is intentionally left alone for
  now because the pages use a live `EDITMODE` tweak-editing mechanism
  (`app/tweaks-panel.jsx`, `postMessage` to a host tool that rewrites the
  source in place) that a bundled build would break. Revisit once that
  workflow is no longer needed.
- **Centralized log aggregation / error tracking**: Serilog now writes to a
  rolling file (`/app/logs` in the container, see the `api_logs` volume) in
  addition to the console, but there's no shipping to Seq/Loki/Sentry yet.
- **Non-root container user**: the api/runner containers still run as the
  image default (root in `api`'s case; the runner's `read_only` + `cap_drop:
  ALL` already covers most of the risk there). Adding a dedicated non-root
  user for `api` is a reasonable next hardening step.
