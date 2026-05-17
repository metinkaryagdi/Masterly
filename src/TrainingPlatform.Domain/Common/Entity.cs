namespace TrainingPlatform.Domain.Common;

public abstract class Entity
{
    protected Entity()
    {
    }

    protected Entity(Guid id, DateTime createdAtUtc)
    {
        Id = id;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; protected set; }

    public DateTime CreatedAtUtc { get; protected set; }

    public DateTime UpdatedAtUtc { get; protected set; }

    public void Touch(DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
    }
}
