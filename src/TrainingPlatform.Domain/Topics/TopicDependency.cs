using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Topics;

public sealed class TopicDependency : Entity
{
    private TopicDependency()
    {
    }

    private TopicDependency(Guid id, Guid topicId, Guid dependsOnTopicId, DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        TopicId = topicId;
        DependsOnTopicId = dependsOnTopicId;
    }

    public Guid TopicId { get; private set; }

    public Guid DependsOnTopicId { get; private set; }

    public static TopicDependency Create(Guid topicId, Guid dependsOnTopicId, DateTime createdAtUtc)
    {
        return new TopicDependency(Guid.NewGuid(), topicId, dependsOnTopicId, createdAtUtc);
    }
}
