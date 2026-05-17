using TrainingPlatform.Application.Common.Models;
using TrainingPlatform.Domain.Questions;

namespace TrainingPlatform.Application.Services;

public interface IQuestionEvaluationService
{
    AnswerEvaluationResult Evaluate(Question question, string? submittedAnswer, Guid? selectedOptionId, int responseTimeSeconds);
}
