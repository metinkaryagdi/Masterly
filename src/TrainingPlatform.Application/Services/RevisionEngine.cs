using TrainingPlatform.Application.Common.Models;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Progress;

namespace TrainingPlatform.Application.Services;

public sealed class RevisionEngine : IRevisionEngine
{
    public RevisionComputation Recalculate(
        TopicProgress progress,
        RevisionSchedule schedule,
        TopicDifficulty difficulty,
        double decayRate,
        AnswerEvaluationResult evaluation,
        DateTime reviewedAtUtc)
    {
        var difficultyWeight = difficulty switch
        {
            TopicDifficulty.Fundamental => 0.7d,
            TopicDifficulty.Intermediate => 0.8d,
            TopicDifficulty.Advanced => 0.9d,
            TopicDifficulty.Expert => 1d,
            _ => 0.8d
        };

        var quality = evaluation.WasCorrect
            ? (0.55d + (evaluation.SpeedScore * 0.2d) + (evaluation.CoverageScore * 0.15d) + (difficultyWeight * 0.1d))
            : (0.1d + (evaluation.SpeedScore * 0.05d) + (evaluation.CoverageScore * 0.1d));

        quality = Math.Clamp(quality, 0.05d, 1d);

        var streakBonus = evaluation.WasCorrect
            ? Math.Min((progress.CurrentCorrectStreak + 1) * 0.03d, 0.15d)
            : -0.08d;

        var masteryScore = (int)Math.Round(
            Math.Clamp((progress.MasteryScore * 0.65d) + ((quality + streakBonus) * 100d * 0.35d), 0d, 100d),
            MidpointRounding.AwayFromZero);

        var consistencyScore = evaluation.WasCorrect
            ? Math.Clamp((progress.ConsistencyScore * 0.75d) + 0.2d + (schedule.LastReviewedAtUtc.HasValue ? 0.05d : 0d), 0d, 1d)
            : Math.Clamp(progress.ConsistencyScore * 0.6d, 0d, 1d);

        var baseIntervalDays = DetermineBaseIntervalDays(masteryScore, evaluation.WasCorrect);
        var adjustedIntervalDays = Math.Max(1, (int)Math.Round(baseIntervalDays / Math.Max(decayRate, 0.4d), MidpointRounding.AwayFromZero));
        var nextReviewAtUtc = reviewedAtUtc.Date.AddDays(adjustedIntervalDays);

        var weakness = 1d - (masteryScore / 100d);
        var forgettingRisk = Math.Clamp(
            (weakness * 0.6d) +
            ((Math.Max(decayRate, 0.4d) - 0.4d) * 0.15d) +
            (evaluation.WasCorrect ? 0d : 0.2d) +
            (evaluation.SpeedScore < 0.7d ? 0.05d : 0d),
            0d,
            1d);

        var priorityScore = Math.Clamp(
            (forgettingRisk * 0.55d) +
            (weakness * 0.25d) +
            (adjustedIntervalDays <= 2 ? 0.1d : 0.03d) +
            (evaluation.WasCorrect ? 0d : 0.1d),
            0d,
            1d);

        return new RevisionComputation(
            masteryScore,
            consistencyScore,
            adjustedIntervalDays,
            nextReviewAtUtc,
            forgettingRisk,
            priorityScore,
            quality);
    }

    private static int DetermineBaseIntervalDays(int masteryScore, bool wasCorrect)
    {
        if (!wasCorrect)
        {
            return 1;
        }

        if (masteryScore < 40)
        {
            return 1;
        }

        if (masteryScore < 55)
        {
            return 2;
        }

        if (masteryScore < 70)
        {
            return 4;
        }

        if (masteryScore < 80)
        {
            return 7;
        }

        if (masteryScore < 90)
        {
            return 14;
        }

        return 30;
    }
}
