using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Domain.Questions;

public sealed class Question : Entity
{
    private Question()
    {
    }

    private Question(
        Guid id,
        Guid topicId,
        QuestionType questionType,
        string prompt,
        string explanation,
        TopicDifficulty difficulty,
        int estimatedSolvingTimeSeconds,
        int minimumPassingScore,
        List<string> tags,
        List<string> acceptedAnswers,
        DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        TopicId = topicId;
        QuestionType = questionType;
        Prompt = prompt;
        Explanation = explanation;
        Difficulty = difficulty;
        EstimatedSolvingTimeSeconds = estimatedSolvingTimeSeconds;
        MinimumPassingScore = minimumPassingScore;
        Tags = tags;
        AcceptedAnswers = acceptedAnswers;
    }

    public Guid TopicId { get; private set; }

    public QuestionType QuestionType { get; private set; }

    public string Prompt { get; private set; } = string.Empty;

    public string Explanation { get; private set; } = string.Empty;

    public TopicDifficulty Difficulty { get; private set; }

    public int EstimatedSolvingTimeSeconds { get; private set; }

    public int MinimumPassingScore { get; private set; }

    public List<string> Tags { get; private set; } = [];

    public List<string> AcceptedAnswers { get; private set; } = [];

    public List<QuestionOption> Options { get; private set; } = [];

    public static Question Create(
        Guid topicId,
        QuestionType questionType,
        string prompt,
        string explanation,
        TopicDifficulty difficulty,
        int estimatedSolvingTimeSeconds,
        int minimumPassingScore,
        IEnumerable<string> tags,
        IEnumerable<string> acceptedAnswers,
        IEnumerable<(string Text, bool IsCorrect)> options,
        DateTime createdAtUtc)
    {
        var question = new Question(
            Guid.NewGuid(),
            topicId,
            questionType,
            prompt.Trim(),
            explanation.Trim(),
            difficulty,
            estimatedSolvingTimeSeconds,
            minimumPassingScore,
            tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            acceptedAnswers.Where(answer => !string.IsNullOrWhiteSpace(answer)).Select(answer => answer.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            createdAtUtc);

        var orderedOptions = options.ToList();
        for (var index = 0; index < orderedOptions.Count; index++)
        {
            question.Options.Add(QuestionOption.Create(question.Id, orderedOptions[index].Text, orderedOptions[index].IsCorrect, index, createdAtUtc));
        }

        return question;
    }
}
