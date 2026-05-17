using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Domain.Challenges;

public sealed class CodingChallenge : Entity
{
    private CodingChallenge()
    {
    }

    private CodingChallenge(
        Guid id,
        Guid topicId,
        string title,
        string description,
        TopicDifficulty difficulty,
        int estimatedMinutes,
        List<string> evaluationCriteria,
        string starterCode,
        string expectedOutcome,
        DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        TopicId = topicId;
        Title = title;
        Description = description;
        Difficulty = difficulty;
        EstimatedMinutes = estimatedMinutes;
        EvaluationCriteria = evaluationCriteria;
        StarterCode = starterCode;
        ExpectedOutcome = expectedOutcome;
        IsActive = true;
    }

    public Guid TopicId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public TopicDifficulty Difficulty { get; private set; }

    public int EstimatedMinutes { get; private set; }

    public List<string> EvaluationCriteria { get; private set; } = [];

    public string StarterCode { get; private set; } = string.Empty;

    public string ExpectedOutcome { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static CodingChallenge Create(
        Guid topicId,
        string title,
        string description,
        TopicDifficulty difficulty,
        int estimatedMinutes,
        IEnumerable<string> evaluationCriteria,
        string starterCode,
        string expectedOutcome,
        DateTime createdAtUtc)
    {
        return new CodingChallenge(
            Guid.NewGuid(),
            topicId,
            title.Trim(),
            description.Trim(),
            difficulty,
            estimatedMinutes,
            evaluationCriteria.Where(criterion => !string.IsNullOrWhiteSpace(criterion)).Select(criterion => criterion.Trim()).ToList(),
            starterCode,
            expectedOutcome,
            createdAtUtc);
    }
}
