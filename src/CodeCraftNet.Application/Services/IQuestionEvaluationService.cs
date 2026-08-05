using CodeCraftNet.Application.Common.Models;
using CodeCraftNet.Domain.Questions;

namespace CodeCraftNet.Application.Services;

public interface IQuestionEvaluationService
{
    AnswerEvaluationResult Evaluate(Question question, string? submittedAnswer, Guid? selectedOptionId, int responseTimeSeconds);
}
