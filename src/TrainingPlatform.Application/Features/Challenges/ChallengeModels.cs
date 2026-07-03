using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Application.Features.Challenges;

public sealed record CodingChallengeDto(
    Guid Id,
    Guid TopicId,
    string Title,
    string Description,
    TopicDifficulty Difficulty,
    int EstimatedMinutes,
    IReadOnlyCollection<string> EvaluationCriteria,
    string StarterCode,
    string ExpectedOutcome,
    bool HasAutomatedTests,
    string TestCode);

public sealed record ScenarioChallengeDto(
    Guid Id,
    Guid TopicId,
    string Title,
    string Scenario,
    TopicDifficulty Difficulty,
    int EstimatedMinutes,
    IReadOnlyCollection<string> EvaluationCriteria,
    string ReferenceSolution);

public sealed record SubmissionDto(
    Guid Id,
    int? Score,
    ChallengeOutcome Outcome,
    DateTime CreatedAtUtc,
    int? TestsPassed,
    int? TestsTotal,
    string Feedback);

/// <summary>Result of a non-persisting "run tests" invocation from the editor.</summary>
public sealed record CodeRunDto(
    bool Evaluated,
    bool Compiled,
    int TotalTests,
    int PassedTests,
    int FailedTests,
    string Output);
