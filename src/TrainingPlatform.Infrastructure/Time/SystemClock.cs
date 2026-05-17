using TrainingPlatform.Application.Abstractions.Time;

namespace TrainingPlatform.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
