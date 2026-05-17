namespace TrainingPlatform.Infrastructure.AI;

public sealed class OllamaOptions
{
    public const string SectionName = "AI:Ollama";

    public bool Enabled { get; init; }

    public string BaseUrl { get; init; } = "http://localhost:11434";

    public string Model { get; init; } = "llama3.1";

    public int TimeoutSeconds { get; init; } = 60;
}
