using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Domain.Topics;

public sealed class Topic : Entity
{
    private Topic()
    {
    }

    private Topic(
        Guid id,
        string name,
        string slug,
        string description,
        TopicDifficulty difficulty,
        double decayRate,
        DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        Name = name;
        Slug = slug;
        Description = description;
        Difficulty = difficulty;
        DecayRate = decayRate;
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public TopicDifficulty Difficulty { get; private set; }

    public double DecayRate { get; private set; }

    public List<TopicDependency> Dependencies { get; private set; } = [];

    public static Topic Create(
        string name,
        string slug,
        string description,
        TopicDifficulty difficulty,
        double decayRate,
        IEnumerable<Guid> dependencyIds,
        DateTime createdAtUtc)
    {
        var topic = new Topic(Guid.NewGuid(), name.Trim(), slug.Trim().ToLowerInvariant(), description.Trim(), difficulty, decayRate, createdAtUtc);
        topic.ReplaceDependencies(dependencyIds, createdAtUtc);
        return topic;
    }

    public void ReplaceDependencies(IEnumerable<Guid> dependencyIds, DateTime updatedAtUtc)
    {
        Dependencies.Clear();

        foreach (var dependencyId in dependencyIds.Distinct().Where(id => id != Id))
        {
            Dependencies.Add(TopicDependency.Create(Id, dependencyId, updatedAtUtc));
        }

        Touch(updatedAtUtc);
    }
}
