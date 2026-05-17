namespace TrainingPlatform.Application.Common.Models;

public sealed record AnswerEvaluationResult(
    bool WasCorrect,
    int Score,
    string NormalizedAnswer,
    string EvaluationSummary,
    double SpeedScore,
    double CoverageScore);
