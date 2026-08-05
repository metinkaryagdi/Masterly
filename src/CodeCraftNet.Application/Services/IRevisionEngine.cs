using CodeCraftNet.Application.Common.Models;
using CodeCraftNet.Domain.Common.Enumerations;
using CodeCraftNet.Domain.Progress;

namespace CodeCraftNet.Application.Services;

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
