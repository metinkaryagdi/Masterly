namespace TrainingPlatform.Application.Common.Models;

public sealed record RevisionComputation(
    int MasteryScore,
    double ConsistencyScore,
    int ReviewIntervalDays,
    DateTime NextReviewAtUtc,
    double ForgettingRisk,
    double PriorityScore,
    double ReviewQuality);
