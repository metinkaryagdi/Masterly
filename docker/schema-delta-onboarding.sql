-- Schema delta for the onboarding feature (goals + topic self-assessments).
--
-- The API creates its schema with EnsureCreated, which only runs against an
-- EMPTY database — it never alters existing tables. Fresh volumes get the new
-- schema automatically; a database created before this feature needs this
-- delta applied once:
--
--   Get-Content docker/schema-delta-onboarding.sql -Raw |
--     docker exec -i training-db psql -U postgres -d training_platform -v ON_ERROR_STOP=1
--
-- Both statements are idempotent; re-running is harmless.

ALTER TABLE user_preferences ADD COLUMN IF NOT EXISTS "Goals" text NOT NULL DEFAULT '[]';
ALTER TABLE user_preferences ALTER COLUMN "Goals" DROP DEFAULT;

CREATE TABLE IF NOT EXISTS topic_self_assessments (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TopicId" uuid NOT NULL,
    "Level" character varying(20) NOT NULL,
    "AssessedAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_topic_self_assessments" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_topic_self_assessments_UserId_TopicId"
    ON topic_self_assessments ("UserId", "TopicId");
