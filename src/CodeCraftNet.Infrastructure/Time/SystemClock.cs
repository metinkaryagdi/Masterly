using CodeCraftNet.Application.Abstractions.Time;

namespace CodeCraftNet.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
