using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Application.Abstractions.AI;

public record QuestionGenerationRequest(
    Guid TopicId,
    string TopicName,
    QuestionType QuestionType,
    TopicDifficulty Difficulty,
    IReadOnlyCollection<string> Tags,
    string PromptVersion);

public record GeneratedQuestion(
    string Prompt,
    string Explanation,
    IReadOnlyCollection<string> AcceptedAnswers,
    IReadOnlyCollection<(string Text, bool IsCorrect)> Options);

public record AnswerEvaluationRequest(
    Guid QuestionId,
    string Prompt,
    string SubmittedAnswer,
    string PromptVersion);

public record CodeFeedbackRequest(
    Guid ChallengeId,
    string ChallengeDescription,
    string SubmittedCode,
    string PromptVersion);

public record ScenarioEvaluationRequest(
    Guid ScenarioChallengeId,
    string Scenario,
    string ResponseText,
    string PromptVersion);

public record AiTextResponse(string Content, string PromptVersion, string Model);

public interface IQuestionGenerationService
{
    Task<GeneratedQuestion> GenerateAsync(QuestionGenerationRequest request, CancellationToken cancellationToken);
}

public interface IAnswerEvaluationService
{
    Task<AiTextResponse> EvaluateAsync(AnswerEvaluationRequest request, CancellationToken cancellationToken);
}

public interface ICodeFeedbackService
{
    Task<AiTextResponse> EvaluateAsync(CodeFeedbackRequest request, CancellationToken cancellationToken);
}

public interface IScenarioEvaluationService
{
    Task<AiTextResponse> EvaluateAsync(ScenarioEvaluationRequest request, CancellationToken cancellationToken);
}
