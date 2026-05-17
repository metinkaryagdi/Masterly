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

        if (await dbContext.Topics.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;

        var csharp = Topic.Create("C# Foundations", "csharp-foundations", "Language fundamentals, LINQ, and async patterns.", TopicDifficulty.Fundamental, 1.2d, [], now);
        var aspNet = Topic.Create("ASP.NET Core API Design", "aspnet-core-api-design", "Controllers, contracts, auth, and HTTP concerns.", TopicDifficulty.Intermediate, 1.15d, [csharp.Id], now);
        var efCore = Topic.Create("EF Core", "ef-core", "Persistence, mappings, and query behavior.", TopicDifficulty.Intermediate, 1.2d, [csharp.Id], now);
        var cleanArchitecture = Topic.Create("Clean Architecture", "clean-architecture", "Boundaries, use cases, and dependency rules.", TopicDifficulty.Advanced, 1.05d, [aspNet.Id, efCore.Id], now);
        var cqrs = Topic.Create("CQRS", "cqrs", "Commands, queries, and behavioral separation.", TopicDifficulty.Advanced, 1.1d, [cleanArchitecture.Id], now);
        var jwt = Topic.Create("JWT Authentication", "jwt-authentication", "Token issuance, validation, and claims design.", TopicDifficulty.Intermediate, 1.1d, [aspNet.Id], now);
        var caching = Topic.Create("Caching Strategy", "caching-strategy", "Read optimization, invalidation, and trade-offs.", TopicDifficulty.Advanced, 1.0d, [aspNet.Id, efCore.Id], now);
        var postgres = Topic.Create("PostgreSQL", "postgresql", "Indexes, transactions, and relational modeling.", TopicDifficulty.Intermediate, 1.15d, [efCore.Id], now);

        await dbContext.Topics.AddRangeAsync([csharp, aspNet, efCore, cleanArchitecture, cqrs, jwt, caching, postgres], cancellationToken);

        var questions = new[]
        {
            Question.Create(csharp.Id, QuestionType.MultipleChoice, "Which LINQ operator projects each element into a new form?", "Select transforms each input element into a new shape.", TopicDifficulty.Fundamental, 60, 100, ["linq"], [], [("Where", false), ("Select", true), ("GroupBy", false)], now),
            Question.Create(csharp.Id, QuestionType.ShortAnswer, "What keyword is used to asynchronously wait for a Task result in C#?", "The await keyword asynchronously resumes when the task completes.", TopicDifficulty.Fundamental, 45, 100, ["async"], ["await"], [], now),
            Question.Create(aspNet.Id, QuestionType.MultipleChoice, "Which middleware must execute before authorization for JWT-secured APIs?", "Authentication populates the user principal before authorization checks policies.", TopicDifficulty.Intermediate, 75, 100, ["auth", "pipeline"], [], [("UseAuthorization", false), ("UseAuthentication", true), ("UseCors", false)], now),
            Question.Create(cleanArchitecture.Id, QuestionType.Scenario, "A controller directly uses DbContext and mapping logic. Which architecture concerns are violated?", "Transport, application, and persistence concerns are leaking into a single layer.", TopicDifficulty.Advanced, 180, 60, ["architecture"], ["boundary", "separation", "dependency", "application"], [], now),
            Question.Create(cqrs.Id, QuestionType.ShortAnswer, "In CQRS, why should read models be separated from command handlers?", "Read paths and write paths have different optimization and consistency concerns.", TopicDifficulty.Advanced, 120, 70, ["cqrs"], ["read optimization", "write model", "separate"], [], now),
            Question.Create(efCore.Id, QuestionType.MultipleChoice, "Which EF Core API is appropriate when you only need read-only query results?", "AsNoTracking avoids change tracker overhead for read-only scenarios.", TopicDifficulty.Intermediate, 60, 100, ["ef-core"], [], [("AsTracking", false), ("AsNoTracking", true), ("Attach", false)], now),
            Question.Create(jwt.Id, QuestionType.ShortAnswer, "Which claim is commonly used as the stable user identifier in ASP.NET Core authorization?", "NameIdentifier is the conventional stable identifier claim for the current principal.", TopicDifficulty.Intermediate, 60, 70, ["jwt"], ["nameidentifier", "sub"], [], now),
            Question.Create(caching.Id, QuestionType.Scenario, "An expensive dashboard query is executed on every request. What trade-offs should guide your caching decision?", "The answer should mention cache freshness, invalidation, read load, and failure modes.", TopicDifficulty.Advanced, 180, 60, ["caching"], ["freshness", "invalidation", "latency", "consistency"], [], now),
            Question.Create(postgres.Id, QuestionType.ShortAnswer, "What kind of PostgreSQL index is usually the default choice for equality lookups?", "B-tree indexes are the general default for equality and range lookups.", TopicDifficulty.Intermediate, 45, 70, ["postgres"], ["btree", "b-tree"], [], now)
        };

        await dbContext.Questions.AddRangeAsync(questions, cancellationToken);

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
}
