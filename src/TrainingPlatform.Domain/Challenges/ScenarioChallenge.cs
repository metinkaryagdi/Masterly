using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Domain.Challenges;

public sealed class ScenarioChallenge : Entity
{
    private ScenarioChallenge()
    {
    }

    private ScenarioChallenge(
        Guid id,
        Guid topicId,
        string title,
        string scenario,
        TopicDifficulty difficulty,
        int estimatedMinutes,
        List<string> evaluationCriteria,
        string referenceSolution,
        DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        TopicId = topicId;
        Title = title;
        Scenario = scenario;
        Difficulty = difficulty;
        EstimatedMinutes = estimatedMinutes;
        EvaluationCriteria = evaluationCriteria;
        ReferenceSolution = referenceSolution;
        IsActive = true;
    }

    public Guid TopicId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Scenario { get; private set; } = string.Empty;

    public TopicDifficulty Difficulty { get; private set; }

    public int EstimatedMinutes { get; private set; }

    public List<string> EvaluationCriteria { get; private set; } = [];

    public string ReferenceSolution { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static ScenarioChallenge Create(
        Guid topicId,
        string title,
        string scenario,
        TopicDifficulty difficulty,
        int estimatedMinutes,
        IEnumerable<string> evaluationCriteria,
        string referenceSolution,
        DateTime createdAtUtc)
    {
        return new ScenarioChallenge(
            Guid.NewGuid(),
            topicId,
            title.Trim(),
            scenario.Trim(),
            difficulty,
            estimatedMinutes,
            evaluationCriteria.Where(criterion => !string.IsNullOrWhiteSpace(criterion)).Select(criterion => criterion.Trim()).ToList(),
            referenceSolution,
            createdAtUtc);
    }
}
