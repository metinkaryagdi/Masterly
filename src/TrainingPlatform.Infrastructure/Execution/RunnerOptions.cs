namespace TrainingPlatform.Infrastructure.Execution;

public sealed class RunnerOptions
{
    public const string SectionName = "Execution:Runner";

    public bool Enabled { get; init; }

    public string BaseUrl { get; init; } = "http://localhost:5055";

    /// <summary>End-to-end HTTP timeout; the runner's own per-test timeout is shorter.</summary>
    public int TimeoutSeconds { get; init; } = 90;
}
