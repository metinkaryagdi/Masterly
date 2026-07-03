# Training Platform

Adaptive training platform for .NET backend skills: daily study plans drawn
from a per-topic question pool, spaced-repetition style revision, topic-level
mastery scoring, and coding/scenario challenges — with a clean-architecture
ASP.NET Core 8 backend and a React (Babel-in-browser) prototype frontend.

## Features

- **Onboarding** — pick goals, set a daily time budget, self-assess every
  topic (Novice / Familiar / Strong). Assessments seed initial mastery
  (20 / 45 / 70) so the very first plan is already personalised.
- **Question pool** — 8 curated questions per topic (64 total) across
  multiple choice, short answer, and scenario types, spread over three
  difficulty levels. Stored in PostgreSQL (`questions` + `question_options`).
- **Daily plans** — each day draws the user's question target from the pool:
  weak topics get 40% of the quota, recently-practised 30%, strong 20%, new
  10%; selection round-robins across topics, targets the difficulty band
  matching current mastery, and holds back questions answered correctly in
  the last 7 days. One coding and one scenario challenge round out the plan.
- **Quiz evaluation** — deterministic scoring (option match, accepted-answer
  match, scenario keyword coverage) updates mastery, streaks, and the
  revision schedule.
- **JWT auth**, per-user preferences (including goals), analytics dashboard.
- **AI hooks** — Ollama-backed question generation / feedback services are
  scaffolded but disabled by default (`AI:Ollama:Enabled`).

## Running with Docker (recommended)

```bash
docker compose up -d --build
```

| Service | URL |
|---|---|
| Frontend (nginx) | http://localhost:8080 |
| API + Swagger | http://localhost:5000/swagger |
| PostgreSQL | localhost:5432 (`postgres`/`postgres`) |

Host ports can be overridden with `WEB_PORT`, `API_PORT`, `DB_PORT` env vars.
On startup the API applies EF Core migrations and tops up seed data
(topics, question pool, challenges) idempotently.

First run: open http://localhost:8080, create an account, and walk through
onboarding — the dashboard, practice, topics, and challenge pages all work
against the live API.

## Running locally without Docker

Requires the .NET 8 SDK and a reachable PostgreSQL (the compose `db` service
works: `docker compose up -d db`).

```bash
dotnet run --project src/TrainingPlatform.Api        # API on http://localhost:5000
# serve the frontend from ./app with any static server, e.g.:
python -m http.server 8080 -d app
```

The frontend pages default to `apiBase: http://localhost:5000` and demo mode
off; each page's Tweaks panel can flip `demoMode` back on to browse with mock
data and no backend.

## Tests

```bash
dotnet test tests/TrainingPlatform.UnitTests/TrainingPlatform.UnitTests.csproj
dotnet test tests/TrainingPlatform.IntegrationTests/TrainingPlatform.IntegrationTests.csproj
```

Integration tests boot the real HTTP pipeline via `WebApplicationFactory`
against in-memory Sqlite — no database or Docker needed.

## Schema changes (EF Core migrations)

The schema evolves through migrations in
`src/TrainingPlatform.Infrastructure/Persistence/Migrations`. To add one:

```bash
dotnet tool restore
dotnet tool run dotnet-ef -- migrations add <Name> \
  --project src/TrainingPlatform.Infrastructure \
  --startup-project src/TrainingPlatform.Infrastructure \
  --output-dir Persistence/Migrations
```

The API applies pending migrations at startup (PostgreSQL only; the Sqlite
test host builds its schema from the model directly). Databases created
before migrations were adopted need the one-time
`docker/baseline-migrations.sql`.

## Adding questions to the pool

Append entries to `QuestionPool()` in
`src/TrainingPlatform.Infrastructure/Seeding/TrainingPlatformSeeder.cs` —
the next API start inserts anything missing (deduped by topic + prompt).
Never reword an existing prompt in place; add a new entry instead.
Questions can also be created at runtime via `POST /api/questions`.

## Project layout

```
app/                              # static frontend (React + Babel in-browser)
src/TrainingPlatform.Domain/          # entities, enums, domain rules
src/TrainingPlatform.Application/     # CQRS handlers, validators, services
src/TrainingPlatform.Infrastructure/  # EF Core, auth, seeding, migrations, AI
src/TrainingPlatform.Api/             # controllers, middleware, composition root
tests/                            # xUnit unit + integration tests
docker/                           # nginx config, one-time SQL helpers
```
