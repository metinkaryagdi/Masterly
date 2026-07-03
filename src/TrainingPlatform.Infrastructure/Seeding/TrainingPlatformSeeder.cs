using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainingPlatform.Domain.Challenges;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Questions;
using TrainingPlatform.Domain.Topics;
using TrainingPlatform.Infrastructure.Persistence;

namespace TrainingPlatform.Infrastructure.Seeding;

public static class TrainingPlatformSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrainingPlatformDbContext>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var now = DateTime.UtcNow;

        if (!await dbContext.Topics.AnyAsync(cancellationToken))
        {
            await SeedTopicsAndChallengesAsync(dbContext, now, cancellationToken);
        }

        // The question pool tops up on every startup so databases created before
        // new pool content was authored pick it up automatically. Existing rows
        // are matched by (topic, prompt), which makes the pass idempotent.
        await TopUpQuestionPoolAsync(dbContext, now, cancellationToken);
    }

    private static async Task SeedTopicsAndChallengesAsync(TrainingPlatformDbContext dbContext, DateTime now, CancellationToken cancellationToken)
    {
        var csharp = Topic.Create("C# Foundations", "csharp-foundations", "Language fundamentals, LINQ, and async patterns.", TopicDifficulty.Fundamental, 1.2d, [], now);
        var aspNet = Topic.Create("ASP.NET Core API Design", "aspnet-core-api-design", "Controllers, contracts, auth, and HTTP concerns.", TopicDifficulty.Intermediate, 1.15d, [csharp.Id], now);
        var efCore = Topic.Create("EF Core", "ef-core", "Persistence, mappings, and query behavior.", TopicDifficulty.Intermediate, 1.2d, [csharp.Id], now);
        var cleanArchitecture = Topic.Create("Clean Architecture", "clean-architecture", "Boundaries, use cases, and dependency rules.", TopicDifficulty.Advanced, 1.05d, [aspNet.Id, efCore.Id], now);
        var cqrs = Topic.Create("CQRS", "cqrs", "Commands, queries, and behavioral separation.", TopicDifficulty.Advanced, 1.1d, [cleanArchitecture.Id], now);
        var jwt = Topic.Create("JWT Authentication", "jwt-authentication", "Token issuance, validation, and claims design.", TopicDifficulty.Intermediate, 1.1d, [aspNet.Id], now);
        var caching = Topic.Create("Caching Strategy", "caching-strategy", "Read optimization, invalidation, and trade-offs.", TopicDifficulty.Advanced, 1.0d, [aspNet.Id, efCore.Id], now);
        var postgres = Topic.Create("PostgreSQL", "postgresql", "Indexes, transactions, and relational modeling.", TopicDifficulty.Intermediate, 1.15d, [efCore.Id], now);

        await dbContext.Topics.AddRangeAsync([csharp, aspNet, efCore, cleanArchitecture, cqrs, jwt, caching, postgres], cancellationToken);

        var codingChallenges = new[]
        {
            CodingChallenge.Create(cqrs.Id, "Add a Query Handler for Topic Mastery", "Design a CQRS query that returns topic mastery metrics without leaking EF entities into the API layer.", TopicDifficulty.Advanced, 60, ["Use an application DTO", "Keep infrastructure concerns out of the API", "Support filtering by topic"], "public sealed record GetTopicMasteryQuery(Guid UserId);", "A query handler and response DTO that returns mastery details cleanly.", now),
            CodingChallenge.Create(aspNet.Id, "Secure a Training Endpoint with JWT", "Add JWT protection and extract the current user identifier from claims for an endpoint.", TopicDifficulty.Intermediate, 45, ["Use Authorize", "Validate claims access", "Avoid trusting request body user ids"], "[Authorize]\npublic sealed class StudyPlansController : ControllerBase { }", "A protected endpoint that reads the user id from the authenticated principal.", now)
        };

        var scenarioChallenges = new[]
        {
            ScenarioChallenge.Create(cleanArchitecture.Id, "Modular Monolith Boundary Review", "You need analytics, revision scheduling, and AI feedback in one deployable. Explain where module boundaries should exist today and what to split later.", TopicDifficulty.Advanced, 45, ["boundary clarity", "data ownership", "future extraction"], "Keep modules separated by application/domain boundaries first, then extract services only when team and deployment pressure justify it.", now),
            ScenarioChallenge.Create(caching.Id, "Caching Dashboard Reads", "A daily dashboard is slow because it joins user progress, schedules, and question history. Explain whether caching belongs here and how invalidation should work.", TopicDifficulty.Advanced, 45, ["cache scope", "freshness", "invalidation"], "Cache read models with explicit invalidation on answer submission and plan generation; avoid caching writes or truth-source state.", now)
        };

        await dbContext.CodingChallenges.AddRangeAsync(codingChallenges, cancellationToken);
        await dbContext.ScenarioChallenges.AddRangeAsync(scenarioChallenges, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task TopUpQuestionPoolAsync(TrainingPlatformDbContext dbContext, DateTime now, CancellationToken cancellationToken)
    {
        var topicIdsBySlug = await dbContext.Topics
            .ToDictionaryAsync(topic => topic.Slug, topic => topic.Id, cancellationToken);

        var existingQuestions = await dbContext.Questions
            .Select(question => new { question.TopicId, question.Prompt })
            .ToListAsync(cancellationToken);
        var existingLookup = existingQuestions
            .Select(entry => (entry.TopicId, entry.Prompt))
            .ToHashSet();

        var added = false;
        foreach (var spec in QuestionPool())
        {
            if (!topicIdsBySlug.TryGetValue(spec.TopicSlug, out var topicId))
            {
                continue;
            }

            if (existingLookup.Contains((topicId, spec.Prompt)))
            {
                continue;
            }

            dbContext.Questions.Add(Question.Create(
                topicId,
                spec.Type,
                spec.Prompt,
                spec.Explanation,
                spec.Difficulty,
                spec.Seconds,
                spec.PassScore,
                spec.Tags,
                spec.Accepted,
                spec.Options,
                now));
            added = true;
        }

        if (added)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record QuestionSpec(
        string TopicSlug,
        QuestionType Type,
        TopicDifficulty Difficulty,
        string Prompt,
        string Explanation,
        int Seconds,
        int PassScore,
        string[] Tags,
        string[] Accepted,
        (string Text, bool IsCorrect)[] Options);

    private static QuestionSpec Mc(string slug, TopicDifficulty difficulty, string prompt, string explanation, int seconds, string[] tags, params (string Text, bool IsCorrect)[] options)
        => new(slug, QuestionType.MultipleChoice, difficulty, prompt, explanation, seconds, 100, tags, [], options);

    private static QuestionSpec Sa(string slug, TopicDifficulty difficulty, string prompt, string explanation, int seconds, string[] tags, params string[] accepted)
        => new(slug, QuestionType.ShortAnswer, difficulty, prompt, explanation, seconds, 70, tags, accepted, []);

    private static QuestionSpec Sc(string slug, TopicDifficulty difficulty, string prompt, string explanation, int seconds, string[] keywords)
        => new(slug, QuestionType.Scenario, difficulty, prompt, explanation, seconds, 60, ["scenario"], keywords, []);

    /// <summary>
    /// The per-topic question pool. Daily plans draw a rotating subset from here,
    /// so each topic should keep roughly 8+ questions across difficulty levels.
    /// Prompts double as identity — never reword an existing prompt in place,
    /// add a new entry instead (the top-up pass dedupes by topic + prompt).
    /// </summary>
    private static IEnumerable<QuestionSpec> QuestionPool()
    {
        const string csharp = "csharp-foundations";
        const string aspnet = "aspnet-core-api-design";
        const string ef = "ef-core";
        const string clean = "clean-architecture";
        const string cqrs = "cqrs";
        const string jwt = "jwt-authentication";
        const string caching = "caching-strategy";
        const string pg = "postgresql";

        // ── C# Foundations ────────────────────────────────────────────────
        yield return Mc(csharp, TopicDifficulty.Fundamental,
            "Which LINQ operator projects each element into a new form?",
            "Select transforms each input element into a new shape.",
            60, ["linq"], ("Where", false), ("Select", true), ("GroupBy", false));
        yield return Sa(csharp, TopicDifficulty.Fundamental,
            "What keyword is used to asynchronously wait for a Task result in C#?",
            "The await keyword asynchronously resumes when the task completes.",
            45, ["async"], "await");
        yield return Mc(csharp, TopicDifficulty.Fundamental,
            "Which of these C# type declarations produces a value type?",
            "Structs (including record structs) live on the stack or inline in containers and are copied by value; classes, interfaces, and delegates are reference types.",
            60, ["types"], ("class Order", false), ("record struct Money(decimal Amount)", true), ("interface IOrder", false), ("delegate void Handler()", false));
        yield return Mc(csharp, TopicDifficulty.Intermediate,
            "What does the property pattern `if (value is { } x)` check?",
            "`is { }` matches any non-null value and introduces a non-nullable variable — it is a concise null check plus cast.",
            75, ["pattern-matching"], ("That value is null", false), ("That value is non-null, binding it to x", true), ("That value is an empty collection", false), ("That value has a default constructor", false));
        yield return Mc(csharp, TopicDifficulty.Intermediate,
            "Two record instances with identical property values are compared with ==. What is the result?",
            "Records generate value-based equality: == compares the property values, not the references.",
            60, ["records"], ("Always false — they are different references", false), ("True — records compare by value", true), ("A compile error — records do not support ==", false));
        yield return Sa(csharp, TopicDifficulty.Fundamental,
            "Which modifier prevents a class from being inherited?",
            "sealed stops further derivation and lets the JIT devirtualize calls.",
            30, ["types"], "sealed");
        yield return Sa(csharp, TopicDifficulty.Intermediate,
            "LINQ queries against IEnumerable<T> do not run until enumerated. What is this behavior called?",
            "Deferred (lazy) execution — the query runs when iterated, not when composed.",
            60, ["linq"], "deferred execution", "deferred", "lazy evaluation", "lazy");
        yield return Sc(csharp, TopicDifficulty.Advanced,
            "A legacy WinForms app calls your async library with .Result and freezes. Explain what is happening and how the library should be written to avoid it.",
            "Blocking on .Result deadlocks when the continuation needs the captured context; libraries should use ConfigureAwait(false) and callers should stay async end-to-end.",
            180, ["deadlock", "configureawait", "async", "blocking"]);

        // ── ASP.NET Core API Design ───────────────────────────────────────
        yield return Mc(aspnet, TopicDifficulty.Intermediate,
            "Which middleware must execute before authorization for JWT-secured APIs?",
            "Authentication populates the user principal before authorization checks policies.",
            75, ["auth", "pipeline"], ("UseAuthorization", false), ("UseAuthentication", true), ("UseCors", false));
        yield return Mc(aspnet, TopicDifficulty.Fundamental,
            "Which binding source does ASP.NET Core use for a complex type parameter on a [HttpPost] controller action by default?",
            "With [ApiController], complex types bind from the JSON request body; simple types bind from route or query.",
            60, ["model-binding"], ("Query string", false), ("Request body", true), ("Route values", false), ("HTTP headers", false));
        yield return Mc(aspnet, TopicDifficulty.Fundamental,
            "A POST endpoint creates a new resource. Which status code best signals success?",
            "201 Created (ideally with a Location header) is the contract for successful resource creation; 200 hides the semantics.",
            45, ["http"], ("200 OK", false), ("201 Created", true), ("204 No Content", false), ("302 Found", false));
        yield return Mc(aspnet, TopicDifficulty.Intermediate,
            "In minimal APIs, what problem does MapGroup solve?",
            "MapGroup attaches a shared route prefix and shared metadata (filters, auth) to a set of endpoints in one place.",
            75, ["minimal-apis"], ("It parallelizes endpoint execution", false), ("It applies a common prefix and shared filters/metadata to related endpoints", true), ("It generates OpenAPI documents", false));
        yield return Sa(aspnet, TopicDifficulty.Fundamental,
            "Which attribute marks a controller or action as requiring an authenticated caller?",
            "[Authorize] gates the endpoint behind the authentication and authorization pipeline.",
            30, ["auth"], "authorize", "[authorize]");
        yield return Sa(aspnet, TopicDifficulty.Intermediate,
            "When a middleware component returns without calling the next delegate, what is that called?",
            "Short-circuiting — the rest of the pipeline never runs, which is how auth failures and static files respond early.",
            60, ["pipeline"], "short-circuit", "short circuiting", "short-circuiting", "terminating the pipeline");
        yield return Sa(aspnet, TopicDifficulty.Advanced,
            "What media type does RFC 7807 define for standardized API error responses?",
            "application/problem+json — ASP.NET Core's ProblemDetails serializes to it.",
            60, ["http", "errors"], "application/problem+json", "problem+json");
        yield return Sc(aspnet, TopicDifficulty.Advanced,
            "You must rename a field in a JSON contract consumed by mobile clients you cannot force-update. Walk through how you would ship this change safely.",
            "Version the contract (or accept both shapes), keep the old field during a deprecation window, and communicate the timeline — never break deployed clients in place.",
            180, ["version", "contract", "backward", "deprecat"]);

        // ── EF Core ───────────────────────────────────────────────────────
        yield return Mc(ef, TopicDifficulty.Intermediate,
            "Which EF Core API is appropriate when you only need read-only query results?",
            "AsNoTracking avoids change tracker overhead for read-only scenarios.",
            60, ["ef-core"], ("AsTracking", false), ("AsNoTracking", true), ("Attach", false));
        yield return Mc(ef, TopicDifficulty.Intermediate,
            "Iterating orders and touching order.Customer.Name fires one extra query per order. What is the standard fix?",
            "That is the N+1 problem — eager-load the relationship with Include (or project only the needed columns) so it becomes a single query.",
            90, ["performance", "n+1"], ("Add AsNoTracking", false), ("Use Include to eager-load Customer", true), ("Wrap the loop in a transaction", false), ("Increase the connection pool size", false));
        yield return Mc(ef, TopicDifficulty.Intermediate,
            "What transactional guarantee does a single SaveChanges call provide?",
            "All changes in that SaveChanges are committed in one implicit transaction — they succeed or roll back together.",
            60, ["transactions"], ("None — each statement commits independently", false), ("All tracked changes commit atomically in one transaction", true), ("Only inserts are transactional", false));
        yield return Mc(ef, TopicDifficulty.Advanced,
            "When do you reach for an EF Core value converter?",
            "Value converters translate between a CLR shape and a column shape — e.g. storing a list as JSON text or an enum as a string.",
            75, ["mapping"], ("To rename a table", false), ("To map a CLR type to a different database representation, like a list serialized to JSON", true), ("To speed up change tracking", false));
        yield return Sa(ef, TopicDifficulty.Fundamental,
            "Which method eagerly loads a related navigation property in a query?",
            "Include (with ThenInclude for deeper levels) joins the related data into the query.",
            45, ["querying"], "include");
        yield return Sa(ef, TopicDifficulty.Advanced,
            "Which operator tells EF Core to run one query per included collection instead of a single join?",
            "AsSplitQuery avoids cartesian explosion when including multiple collections.",
            60, ["performance"], "assplitquery", "split query");
        yield return Sa(ef, TopicDifficulty.Intermediate,
            "What is the DbContext component that records entity states (Added, Modified, Deleted) called?",
            "The change tracker — SaveChanges reads it to produce SQL.",
            45, ["ef-core"], "change tracker", "changetracker");
        yield return Sc(ef, TopicDifficulty.Advanced,
            "A dashboard query got slow as data grew. Describe your diagnosis steps and the levers you would consider in EF Core and the database.",
            "Capture the generated SQL, EXPLAIN it, check indexes, project only needed columns, disable tracking for reads, and consider split queries or moving aggregation into the database.",
            240, ["index", "tracking", "projection", "sql"]);

        // ── Clean Architecture ────────────────────────────────────────────
        yield return Sc(clean, TopicDifficulty.Advanced,
            "A controller directly uses DbContext and mapping logic. Which architecture concerns are violated?",
            "Transport, application, and persistence concerns are leaking into a single layer.",
            180, ["boundary", "separation", "dependency", "application"]);
        yield return Mc(clean, TopicDifficulty.Fundamental,
            "In Clean Architecture, which direction must source-code dependencies point?",
            "Dependencies point inward: outer layers (UI, infrastructure) depend on inner layers (application, domain), never the reverse.",
            60, ["dependencies"], ("Outward, toward infrastructure", false), ("Inward, toward the domain", true), ("Both directions are fine if interfaces are used", false));
        yield return Mc(clean, TopicDifficulty.Fundamental,
            "Where do use cases (application-specific business rules) live?",
            "The application layer orchestrates use cases; the domain holds enterprise rules, and outer layers only adapt.",
            60, ["layers"], ("Domain layer", false), ("Application layer", true), ("Infrastructure layer", false), ("API layer", false));
        yield return Mc(clean, TopicDifficulty.Intermediate,
            "Your application layer needs persistence. Where do the repository interface and its EF Core implementation belong?",
            "The interface is owned by the inner layer that consumes it; the implementation lives in infrastructure — that inversion keeps the core persistence-agnostic.",
            90, ["dependencies"], ("Both in infrastructure", false), ("Interface in application/domain, implementation in infrastructure", true), ("Interface in the API layer, implementation in the domain", false));
        yield return Mc(clean, TopicDifficulty.Intermediate,
            "What is the primary job of a DTO at an architectural boundary?",
            "DTOs decouple the wire/persistence shape from domain objects so inner models can evolve without breaking contracts.",
            60, ["boundaries"], ("Enforce business invariants", false), ("Carry data across a boundary without exposing domain internals", true), ("Cache query results", false));
        yield return Sa(clean, TopicDifficulty.Fundamental,
            "Which SOLID principle says high-level modules should depend on abstractions rather than concrete implementations?",
            "The Dependency Inversion Principle — the D in SOLID and the mechanism behind Clean Architecture's inward-pointing dependencies.",
            45, ["solid"], "dependency inversion", "dependency inversion principle", "dip");
        yield return Sa(clean, TopicDifficulty.Fundamental,
            "In which layer do EF Core entity configurations and migrations belong?",
            "Infrastructure — persistence mapping is an implementation detail hidden behind application-owned abstractions.",
            45, ["layers"], "infrastructure");
        yield return Sa(clean, TopicDifficulty.Intermediate,
            "What do you call a small immutable domain object whose identity is defined entirely by its values (e.g. Money, DateRange)?",
            "A value object — equal when its values are equal, with no identity of its own.",
            60, ["domain"], "value object");

        // ── CQRS ──────────────────────────────────────────────────────────
        yield return Sa(cqrs, TopicDifficulty.Advanced,
            "In CQRS, why should read models be separated from command handlers?",
            "Read paths and write paths have different optimization and consistency concerns.",
            120, ["cqrs"], "read optimization", "write model", "separate");
        yield return Mc(cqrs, TopicDifficulty.Fundamental,
            "What is the defining difference between a command and a query?",
            "Commands change state and return little or nothing; queries return data and must not change state.",
            45, ["cqrs"], ("Commands are faster than queries", false), ("Commands mutate state; queries only read it", true), ("Queries run on a different thread", false));
        yield return Mc(cqrs, TopicDifficulty.Intermediate,
            "Where does input validation belong in a CQRS pipeline?",
            "In a pipeline step (validator/behavior) that runs before the handler, so handlers only ever see valid commands.",
            75, ["validation"], ("Inside each controller action", false), ("In a pipeline behavior before the handler executes", true), ("In the database via constraints only", false));
        yield return Mc(cqrs, TopicDifficulty.Advanced,
            "You add a separate denormalized read store fed by events. What consistency property must the UI now tolerate?",
            "Eventual consistency — the read store lags the write store briefly, so reads may not reflect the latest write.",
            90, ["consistency"], ("Strong consistency", false), ("Eventual consistency", true), ("Serializable isolation", false));
        yield return Sa(cqrs, TopicDifficulty.Fundamental,
            "What is the component called that receives a command and routes it to its single handler?",
            "A dispatcher (or mediator) resolves the handler for a command and invokes it.",
            45, ["cqrs"], "dispatcher", "mediator", "command dispatcher");
        yield return Sa(cqrs, TopicDifficulty.Intermediate,
            "Cross-cutting concerns like logging and validation wrap every handler via what pattern in MediatR-style pipelines?",
            "Pipeline behaviors — decorators that compose around the handler call.",
            60, ["pipeline"], "pipeline behavior", "behavior", "decorator");
        yield return Sa(cqrs, TopicDifficulty.Fundamental,
            "What does the acronym CQRS stand for?",
            "Command Query Responsibility Segregation.",
            30, ["cqrs"], "command query responsibility segregation");
        yield return Sc(cqrs, TopicDifficulty.Advanced,
            "A teammate insists every read must go through the full domain model for purity. Argue the CQRS position for a high-traffic list endpoint.",
            "Reads can bypass the domain: project straight to DTOs from a read model for performance; invariants only matter on the write side.",
            180, ["read model", "projection", "dto", "performance"]);

        // ── JWT Authentication ────────────────────────────────────────────
        yield return Sa(jwt, TopicDifficulty.Intermediate,
            "Which claim is commonly used as the stable user identifier in ASP.NET Core authorization?",
            "NameIdentifier is the conventional stable identifier claim for the current principal.",
            60, ["jwt"], "nameidentifier", "sub");
        yield return Mc(jwt, TopicDifficulty.Fundamental,
            "What are the three parts of a JWT, in order?",
            "header.payload.signature — two base64url-encoded JSON segments plus a signature over them.",
            45, ["jwt"], ("Header, payload, signature", true), ("Issuer, audience, secret", false), ("Claims, scopes, roles", false));
        yield return Mc(jwt, TopicDifficulty.Intermediate,
            "Where is the safest conventional place for a browser SPA to keep a refresh token?",
            "An HttpOnly, Secure cookie — JavaScript cannot read it, which blunts XSS token theft; localStorage is readable by any injected script.",
            90, ["security"], ("localStorage", false), ("An HttpOnly Secure cookie", true), ("A global JavaScript variable", false), ("The URL fragment", false));
        yield return Mc(jwt, TopicDifficulty.Intermediate,
            "What does a JWT signature actually guarantee?",
            "Integrity and authenticity — the payload was not altered and was issued by a key holder. It does NOT encrypt the payload; anyone can read it.",
            75, ["security"], ("The payload is encrypted", false), ("The token has not been tampered with and comes from the key holder", true), ("The token cannot be replayed", false));
        yield return Sa(jwt, TopicDifficulty.Fundamental,
            "Which registered JWT claim carries the expiration time?",
            "exp — a Unix timestamp after which validation must fail.",
            30, ["jwt"], "exp");
        yield return Sa(jwt, TopicDifficulty.Intermediate,
            "HS256 signs tokens with what kind of cryptographic scheme?",
            "A symmetric HMAC — the same shared secret signs and verifies, unlike RS256's public/private pair.",
            60, ["crypto"], "hmac", "symmetric");
        yield return Sa(jwt, TopicDifficulty.Fundamental,
            "In which HTTP header does a client conventionally send a bearer token?",
            "Authorization: Bearer <token>.",
            30, ["http"], "authorization", "authorization: bearer");
        yield return Sc(jwt, TopicDifficulty.Advanced,
            "Access tokens for your API were leaked through a logging bug. Describe your immediate containment steps and the design changes that limit future blast radius.",
            "Short expiry limits exposure; rotate signing keys and revoke refresh tokens now; add refresh-token rotation with reuse detection, keep tokens out of logs, and require https everywhere.",
            240, ["rotation", "revoke", "expiry", "https"]);

        // ── Caching Strategy ──────────────────────────────────────────────
        yield return Sc(caching, TopicDifficulty.Advanced,
            "An expensive dashboard query is executed on every request. What trade-offs should guide your caching decision?",
            "The answer should mention cache freshness, invalidation, read load, and failure modes.",
            180, ["freshness", "invalidation", "latency", "consistency"]);
        yield return Mc(caching, TopicDifficulty.Intermediate,
            "Which sequence describes the cache-aside pattern?",
            "The application checks the cache first, loads from the source on a miss, then writes the result back into the cache.",
            75, ["patterns"], ("Write to cache and database simultaneously on every write", false), ("Check cache; on miss load from source and populate the cache", true), ("The cache itself lazily queries the database", false));
        yield return Mc(caching, TopicDifficulty.Advanced,
            "A hot cache key expires and 500 concurrent requests hammer the database rebuilding it. Which technique prevents this stampede?",
            "Lock/single-flight the rebuild so one caller recomputes while others wait or serve stale — jittered TTLs also help spread expiry.",
            90, ["stampede"], ("Shorter TTLs on every key", false), ("Single-flight locking so only one request recomputes the value", true), ("Caching the value in two caches", false));
        yield return Mc(caching, TopicDifficulty.Fundamental,
            "Which of these is the safest candidate for aggressive caching?",
            "Slowly-changing reference data (e.g. a country list) tolerates long TTLs; per-user balances and inventory counts go stale dangerously fast.",
            60, ["strategy"], ("A user's account balance", false), ("A country/currency reference list", true), ("Live inventory counts during a sale", false));
        yield return Sa(caching, TopicDifficulty.Fundamental,
            "Which eviction policy removes the entry that has gone unused the longest?",
            "LRU — least recently used.",
            30, ["eviction"], "lru", "least recently used");
        yield return Sa(caching, TopicDifficulty.Intermediate,
            "Which HTTP response header controls how clients and proxies may cache a response?",
            "Cache-Control (max-age, no-store, public/private…).",
            45, ["http"], "cache-control");
        yield return Sa(caching, TopicDifficulty.Fundamental,
            "Name the in-memory data store most commonly used as a distributed cache with .NET.",
            "Redis — via IDistributedCache or StackExchange.Redis.",
            30, ["redis"], "redis");
        yield return Mc(caching, TopicDifficulty.Advanced,
            "In write-through caching, what happens on a write?",
            "The write goes to the cache and the underlying store together, keeping them consistent at the cost of write latency.",
            75, ["patterns"], ("Only the cache is updated; the store syncs later", false), ("Cache and backing store are updated together synchronously", true), ("The cache entry is deleted and lazily reloaded", false));

        // ── PostgreSQL ────────────────────────────────────────────────────
        yield return Sa(pg, TopicDifficulty.Intermediate,
            "What kind of PostgreSQL index is usually the default choice for equality lookups?",
            "B-tree indexes are the general default for equality and range lookups.",
            45, ["postgres"], "btree", "b-tree");
        yield return Mc(pg, TopicDifficulty.Intermediate,
            "What does EXPLAIN ANALYZE do that plain EXPLAIN does not?",
            "It actually executes the query and reports real timings and row counts alongside the plan — invaluable, but beware running it on writes.",
            75, ["performance"], ("Formats the plan as JSON", false), ("Executes the query and shows actual timings and row counts", true), ("Analyzes table statistics for the planner", false));
        yield return Mc(pg, TopicDifficulty.Intermediate,
            "Which transaction isolation level does PostgreSQL use by default?",
            "Read Committed — each statement sees data committed before that statement began.",
            60, ["transactions"], ("Serializable", false), ("Read Committed", true), ("Repeatable Read", false), ("Read Uncommitted", false));
        yield return Mc(pg, TopicDifficulty.Advanced,
            "When is a partial index the right tool?",
            "When queries only ever touch a predictable slice of rows (e.g. WHERE status = 'active') — the index stays small and writes to other rows skip it.",
            90, ["indexes"], ("When the table is small", false), ("When queries filter on a fixed predicate matching a subset of rows", true), ("When you need uniqueness across all rows", false));
        yield return Sa(pg, TopicDifficulty.Fundamental,
            "Which SQL command shows the execution plan the PostgreSQL planner chose for a query?",
            "EXPLAIN (optionally with ANALYZE to run it for real numbers).",
            30, ["performance"], "explain");
        yield return Sa(pg, TopicDifficulty.Advanced,
            "Which index type serves jsonb containment queries (the @> operator) efficiently?",
            "GIN — generalized inverted indexes handle multi-value containment for jsonb and arrays.",
            60, ["indexes", "jsonb"], "gin");
        yield return Sa(pg, TopicDifficulty.Intermediate,
            "What does MVCC stand for in PostgreSQL's concurrency model?",
            "Multiversion Concurrency Control — readers see snapshots instead of blocking writers.",
            45, ["concurrency"], "multiversion concurrency control", "multi-version concurrency control");
        yield return Sc(pg, TopicDifficulty.Advanced,
            "A heavily-updated table keeps growing on disk and scans get slower even though row count is stable. Explain what is happening and how PostgreSQL deals with it.",
            "Updates leave dead tuples behind under MVCC — that is table bloat; VACUUM (and a properly tuned autovacuum) reclaims dead space, and fillfactor/HOT updates reduce churn.",
            240, ["vacuum", "dead", "autovacuum", "bloat"]);
    }
}
