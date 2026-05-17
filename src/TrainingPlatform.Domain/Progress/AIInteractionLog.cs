using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Progress;

public sealed class AIInteractionLog : Entity
{
    private AIInteractionLog()
    {
    }

    private AIInteractionLog(
        Guid id,
        Guid? userId,
        string operationType,
        string provider,
        string model,
        string promptVersion,
        string promptHash,
        string requestSummary,
        string responseSummary,
        int latencyMs,
        bool wasSuccessful,
        DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        UserId = userId;
        OperationType = operationType;
        Provider = provider;
        Model = model;
        PromptVersion = promptVersion;
        PromptHash = promptHash;
        RequestSummary = requestSummary;
        ResponseSummary = responseSummary;
        LatencyMs = latencyMs;
        WasSuccessful = wasSuccessful;
    }

    public Guid? UserId { get; private set; }

    public string OperationType { get; private set; } = string.Empty;

    public string Provider { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public string PromptVersion { get; private set; } = string.Empty;

    public string PromptHash { get; private set; } = string.Empty;

    public string RequestSummary { get; private set; } = string.Empty;

    public string ResponseSummary { get; private set; } = string.Empty;

    public int LatencyMs { get; private set; }

    public bool WasSuccessful { get; private set; }

    public static AIInteractionLog Create(
        Guid? userId,
        string operationType,
        string provider,
        string model,
        string promptVersion,
        string promptHash,
        string requestSummary,
        string responseSummary,
        int latencyMs,
        bool wasSuccessful,
        DateTime createdAtUtc)
    {
        return new AIInteractionLog(
            Guid.NewGuid(),
            userId,
            operationType,
            provider,
            model,
            promptVersion,
            promptHash,
            requestSummary,
            responseSummary,
            latencyMs,
            wasSuccessful,
            createdAtUtc);
    }
}
