using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TrainingPlatform.Application.Abstractions.Execution;
using TrainingPlatform.Application.Features.Challenges;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Infrastructure.Persistence;

namespace TrainingPlatform.IntegrationTests;

/// <summary>Scripted judge: returns whatever the current test put in <see cref="Next"/>.</summary>
public sealed class StubCodeExecutionService : ICodeExecutionService
{
    public static CodeExecutionResult Next { get; set; } = new(true, 4, 4, 0, "All 4 tests passed.", 100);

    public bool IsEnabled => true;

    public Task<CodeExecutionResult> RunAsync(string solutionCode, string testCode, CancellationToken cancellationToken)
        => Task.FromResult(Next);
}

public sealed class StubRunnerApiFactory : TrainingPlatformApiFactory
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<ICodeExecutionService>();
        services.AddSingleton<ICodeExecutionService, StubCodeExecutionService>();
    }
}

public sealed class ChallengeExecutionTests(StubRunnerApiFactory factory) : IClassFixture<StubRunnerApiFactory>
{
    private async Task<Guid> RunnableChallengeIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrainingPlatformDbContext>();
        var challenge = await dbContext.CodingChallenges.FirstAsync(entry => entry.TestCode != "");
        return challenge.Id;
    }

    private static object SubmissionBody(Guid challengeId) => new
    {
        codingChallengeId = challengeId,
        dailyStudyPlanId = (Guid?)null,
        submittedCode = "public class Solution { }",
        notes = "",
    };

    [Fact]
    public async Task All_tests_passing_scores_100_and_records_progress()
    {
        StubCodeExecutionService.Next = new CodeExecutionResult(true, 4, 4, 0, "All 4 tests passed.", 120);
        var client = factory.CreateClient();
        var auth = await ApiFlows.RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var challengeId = await RunnableChallengeIdAsync();

        var response = await client.PostAsJsonAsync("/api/challenges/coding/submissions", SubmissionBody(challengeId));

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<SubmissionDto>(ApiFlows.Json);
        Assert.NotNull(dto);
        Assert.Equal(100, dto.Score);
        Assert.Equal(ChallengeOutcome.Passed, dto.Outcome);
        Assert.Equal(4, dto.TestsPassed);
        Assert.Equal(4, dto.TestsTotal);
        Assert.Contains("passed", dto.Feedback, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrainingPlatformDbContext>();
        var challenge = await dbContext.CodingChallenges.SingleAsync(entry => entry.Id == challengeId);
        var progress = await dbContext.TopicProgressEntries
            .SingleAsync(entry => entry.UserId == auth.UserId && entry.TopicId == challenge.TopicId);
        Assert.Equal(1, progress.CodingChallengeAttempts);
        Assert.Equal(1, progress.CodingChallengeSuccesses);
    }

    [Fact]
    public async Task Partial_pass_scores_the_ratio_and_needs_work()
    {
        StubCodeExecutionService.Next = new CodeExecutionResult(true, 4, 1, 3, "1 of 4 tests passed.", 150);
        var client = await ApiFlows.RegisteredClientAsync(factory);
        var challengeId = await RunnableChallengeIdAsync();

        var response = await client.PostAsJsonAsync("/api/challenges/coding/submissions", SubmissionBody(challengeId));

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<SubmissionDto>(ApiFlows.Json);
        Assert.NotNull(dto);
        Assert.Equal(25, dto.Score);
        Assert.Equal(ChallengeOutcome.NeedsWork, dto.Outcome);
    }

    [Fact]
    public async Task Compile_failure_scores_zero_with_the_error_in_feedback()
    {
        StubCodeExecutionService.Next = new CodeExecutionResult(false, 0, 0, 0, "Compilation failed.\nerror CS1002: ; expected", 80);
        var client = await ApiFlows.RegisteredClientAsync(factory);
        var challengeId = await RunnableChallengeIdAsync();

        var response = await client.PostAsJsonAsync("/api/challenges/coding/submissions", SubmissionBody(challengeId));

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<SubmissionDto>(ApiFlows.Json);
        Assert.NotNull(dto);
        Assert.Equal(0, dto.Score);
        Assert.Equal(ChallengeOutcome.NeedsWork, dto.Outcome);
        Assert.Contains("Compilation failed", dto.Feedback);
    }

    [Fact]
    public async Task Run_endpoint_reports_results_without_recording_a_submission()
    {
        StubCodeExecutionService.Next = new CodeExecutionResult(true, 4, 2, 2, "2 of 4 tests passed.", 90);
        var client = await ApiFlows.RegisteredClientAsync(factory);
        var challengeId = await RunnableChallengeIdAsync();

        int CountSubmissions()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TrainingPlatformDbContext>();
            return dbContext.CodingSubmissions.Count();
        }

        var before = CountSubmissions();
        var response = await client.PostAsJsonAsync($"/api/challenges/coding/{challengeId}/run", new { submittedCode = "public class X { }" });

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<CodeRunDto>(ApiFlows.Json);
        Assert.NotNull(dto);
        Assert.True(dto.Evaluated);
        Assert.True(dto.Compiled);
        Assert.Equal(2, dto.PassedTests);
        Assert.Equal(before, CountSubmissions());
    }
}

/// <summary>Behavior when no runner is configured (the default factory).</summary>
public sealed class ChallengeExecutionDisabledTests(TrainingPlatformApiFactory factory) : IClassFixture<TrainingPlatformApiFactory>
{
    [Fact]
    public async Task Coding_submission_without_a_runner_stays_pending_review()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);

        Guid challengeId;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TrainingPlatformDbContext>();
            challengeId = (await dbContext.CodingChallenges.FirstAsync(entry => entry.TestCode != "")).Id;
        }

        var response = await client.PostAsJsonAsync("/api/challenges/coding/submissions", new
        {
            codingChallengeId = challengeId,
            submittedCode = "public class Solution { }",
            notes = "",
        });

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<SubmissionDto>(ApiFlows.Json);
        Assert.NotNull(dto);
        Assert.Equal(ChallengeOutcome.PendingReview, dto.Outcome);
        Assert.Null(dto.Score);
    }

    [Fact]
    public async Task Scenario_submission_scores_criteria_coverage_deterministically()
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);

        Guid challengeId;
        IReadOnlyList<string> criteria;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TrainingPlatformDbContext>();
            var challenge = await dbContext.ScenarioChallenges.FirstAsync();
            challengeId = challenge.Id;
            criteria = challenge.EvaluationCriteria;
        }

        var fullAnswer = "My analysis: " + string.Join(". Also consider ", criteria) + ".";
        var goodResponse = await client.PostAsJsonAsync("/api/challenges/scenario/submissions", new
        {
            scenarioChallengeId = challengeId,
            responseText = fullAnswer,
        });
        goodResponse.EnsureSuccessStatusCode();
        var good = await goodResponse.Content.ReadFromJsonAsync<SubmissionDto>(ApiFlows.Json);
        Assert.NotNull(good);
        Assert.Equal(100, good.Score);
        Assert.Equal(ChallengeOutcome.Passed, good.Outcome);

        var weakResponse = await client.PostAsJsonAsync("/api/challenges/scenario/submissions", new
        {
            scenarioChallengeId = challengeId,
            responseText = "I would just add more servers.",
        });
        weakResponse.EnsureSuccessStatusCode();
        var weak = await weakResponse.Content.ReadFromJsonAsync<SubmissionDto>(ApiFlows.Json);
        Assert.NotNull(weak);
        Assert.Equal(0, weak.Score);
        Assert.Equal(ChallengeOutcome.NeedsWork, weak.Outcome);
        Assert.Contains("Not covered yet", weak.Feedback);
    }
}
