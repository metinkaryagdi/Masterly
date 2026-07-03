-- One-time baseline for databases created before EF Core migrations existed
-- (i.e. via EnsureCreated). The schema already matches the InitialCreate
-- migration, so we only record it as applied; from then on the API's
-- MigrateAsync applies future migrations normally.
--
--   Get-Content docker/baseline-migrations.sql -Raw |
--     docker exec -i training-db psql -U postgres -d training_platform -v ON_ERROR_STOP=1
--
-- Fresh/empty databases do NOT need this — MigrateAsync creates everything.
-- Idempotent; re-running is harmless.

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260703132510_InitialCreate', '8.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260703132510_InitialCreate'
);
