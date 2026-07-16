using TrainingPlatform.Application.Abstractions.Execution;
using TrainingPlatform.Application.Services;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.UnitTests.Services;

public sealed class ChallengeEvaluationTests
{
    [Fact]
    public void Code_run_with_all_tests_green_scores_100_and_passes()
    {
        var run = new CodeExecutionResult(true, 5, 5, 0, "5 testin tamamı geçti.", 200);

        var (score, outcome, feedback) = ChallengeEvaluation.ForCodeRun(run);

        Assert.Equal(100, score);
        Assert.Equal(ChallengeOutcome.Passed, outcome);
        Assert.Equal("5 testin tamamı geçti.", feedback);
    }

    [Theory]
    [InlineData(5, 3, 2, 60)]
    [InlineData(4, 1, 3, 25)]
    [InlineData(3, 0, 3, 0)]
    public void Code_run_with_failures_scores_the_pass_ratio(int total, int passed, int failed, int expectedScore)
    {
        var run = new CodeExecutionResult(true, total, passed, failed, "output", 200);

        var (score, outcome, _) = ChallengeEvaluation.ForCodeRun(run);

        Assert.Equal(expectedScore, score);
        Assert.Equal(ChallengeOutcome.NeedsWork, outcome);
    }

    [Fact]
    public void Compile_failure_scores_zero_and_surfaces_the_compiler_output()
    {
        var run = new CodeExecutionResult(false, 0, 0, 0, "Derleme başarısız oldu.\nerror CS1002", 90);

        var (score, outcome, feedback) = ChallengeEvaluation.ForCodeRun(run);

        Assert.Equal(0, score);
        Assert.Equal(ChallengeOutcome.NeedsWork, outcome);
        Assert.Contains("Derleme başarısız", feedback);
    }

    [Fact]
    public void Empty_test_suite_never_passes()
    {
        var run = new CodeExecutionResult(true, 0, 0, 0, "", 50);

        var (score, outcome, _) = ChallengeEvaluation.ForCodeRun(run);

        Assert.Equal(0, score);
        Assert.Equal(ChallengeOutcome.NeedsWork, outcome);
    }

    [Fact]
    public void Scenario_response_covering_every_criterion_passes_with_full_score()
    {
        var criteria = new[] { "cache scope", "freshness", "invalidation" };
        var response = "Define the CACHE SCOPE first, balance freshness, and plan invalidation on writes.";

        var (score, outcome, feedback) = ChallengeEvaluation.ForScenarioResponse(criteria, response);

        Assert.Equal(100, score);
        Assert.Equal(ChallengeOutcome.Passed, outcome);
        Assert.Contains("3 değerlendirme kriterinin tamamı", feedback);
    }

    [Fact]
    public void Scenario_response_with_partial_coverage_below_threshold_needs_work()
    {
        var criteria = new[] { "cache scope", "freshness", "invalidation" };
        var response = "I would think about freshness only.";

        var (score, outcome, feedback) = ChallengeEvaluation.ForScenarioResponse(criteria, response);

        Assert.Equal(33, score);
        Assert.Equal(ChallengeOutcome.NeedsWork, outcome);
        Assert.Contains("cache scope", feedback);
        Assert.Contains("invalidation", feedback);
    }

    [Fact]
    public void Scenario_with_no_criteria_stays_pending()
    {
        var (_, outcome, _) = ChallengeEvaluation.ForScenarioResponse([], "anything");

        Assert.Equal(ChallengeOutcome.PendingReview, outcome);
    }
}
