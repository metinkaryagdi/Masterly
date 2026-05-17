using TrainingPlatform.Application.Common.Models;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Progress;

namespace TrainingPlatform.Application.Services;

public interface IRevisionEngine
{
    RevisionComputation Recalculate(
        TopicProgress progress,
        RevisionSchedule schedule,
        TopicDifficulty difficulty,
        double decayRate,
        AnswerEvaluationResult evaluation,
        DateTime reviewedAtUtc);
}
