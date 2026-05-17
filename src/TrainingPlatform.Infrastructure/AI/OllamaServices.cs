using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TrainingPlatform.Application.Abstractions.AI;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Domain.Progress;

namespace TrainingPlatform.Infrastructure.AI;

internal sealed record OllamaGenerateRequest(string Model, string Prompt, bool Stream = false);

internal sealed record OllamaGenerateResponse(string Response);

internal sealed class OllamaApiClient(HttpClient httpClient, IOptions<OllamaOptions> options)
{
    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        var ollamaOptions = options.Value;
        if (!ollamaOptions.Enabled)
        {
            throw new InvalidOperationException("Ollama integration is disabled.");
        }

        httpClient.BaseAddress = new Uri(ollamaOptions.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(ollamaOptions.TimeoutSeconds);

        var response = await httpClient.PostAsJsonAsync(
            "/api/generate",
            new OllamaGenerateRequest(ollamaOptions.Model, prompt),
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken);
        return payload?.Response ?? string.Empty;
    }
}

internal static class PromptCatalog
{
    public static string BuildQuestionGenerationPrompt(QuestionGenerationRequest request)
    {
        return $$"""
                 You are generating deterministic training content support output.
                 Return valid JSON with properties:
                 prompt, explanation, acceptedAnswers, options.
                 Topic: {{request.TopicName}}
                 QuestionType: {{request.QuestionType}}
                 Difficulty: {{request.Difficulty}}
                 Tags: {{string.Join(", ", request.Tags)}}
                 PromptVersion: {{request.PromptVersion}}
                 """;
    }

    public static string BuildAnswerEvaluationPrompt(AnswerEvaluationRequest request)
    {
        return $$"""
                 Explain the submitted answer at coaching level.
                 Do not provide a score. Keep it concise.
                 PromptVersion: {{request.PromptVersion}}
                 Question: {{request.Prompt}}
                 SubmittedAnswer: {{request.SubmittedAnswer}}
                 """;
    }

    public static string BuildCodeFeedbackPrompt(CodeFeedbackRequest request)
    {
        return $$"""
                 Review this backend coding submission and provide feedback only.
                 Do not assign deterministic truth. Keep it concise.
                 PromptVersion: {{request.PromptVersion}}
                 Challenge: {{request.ChallengeDescription}}
                 SubmittedCode:
                 {{request.SubmittedCode}}
                 """;
    }

    public static string BuildScenarioFeedbackPrompt(ScenarioEvaluationRequest request)
    {
        return $$"""
                 Review the architecture response and provide feedback only.
                 Highlight trade-offs and missed considerations.
                 PromptVersion: {{request.PromptVersion}}
                 Scenario: {{request.Scenario}}
                 Response:
                 {{request.ResponseText}}
                 """;
    }
}

internal sealed class OllamaQuestionGenerationService(
    OllamaApiClient client,
    ITrainingPlatformDbContext dbContext,
    IClock clock,
    IOptions<OllamaOptions> options) : IQuestionGenerationService
{
    public async Task<GeneratedQuestion> GenerateAsync(QuestionGenerationRequest request, CancellationToken cancellationToken)
    {
        var prompt = PromptCatalog.BuildQuestionGenerationPrompt(request);
        var stopwatch = Stopwatch.StartNew();
        var response = await client.GenerateAsync(prompt, cancellationToken);
        stopwatch.Stop();

        await LogAsync(request.TopicId, "QuestionGeneration", request.PromptVersion, prompt, response, stopwatch.ElapsedMilliseconds, true, cancellationToken);

        var payload = JsonSerializer.Deserialize<GeneratedQuestionPayload>(response, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                      ?? throw new InvalidOperationException("Ollama returned an invalid question payload.");

        return new GeneratedQuestion(payload.Prompt, payload.Explanation, payload.AcceptedAnswers, payload.Options.Select(option => (option.Text, option.IsCorrect)).ToList());
    }

    private async Task LogAsync(Guid topicId, string operationType, string promptVersion, string prompt, string response, long latencyMs, bool wasSuccessful, CancellationToken cancellationToken)
    {
        dbContext.AIInteractionLogs.Add(AIInteractionLog.Create(
            null,
            operationType,
            "Ollama",
            options.Value.Model,
            promptVersion,
            OllamaHashing.ComputeHash(prompt),
            $"topic:{topicId}",
            response[..Math.Min(response.Length, 3000)],
            (int)latencyMs,
            wasSuccessful,
            clock.UtcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class OllamaAnswerEvaluationService(
    OllamaApiClient client,
    ITrainingPlatformDbContext dbContext,
    IClock clock,
    IOptions<OllamaOptions> options) : IAnswerEvaluationService
{
    public async Task<AiTextResponse> EvaluateAsync(AnswerEvaluationRequest request, CancellationToken cancellationToken)
    {
        var prompt = PromptCatalog.BuildAnswerEvaluationPrompt(request);
        var stopwatch = Stopwatch.StartNew();
        var response = await client.GenerateAsync(prompt, cancellationToken);
        stopwatch.Stop();

        await LogAsync(request.QuestionId, "AnswerEvaluation", request.PromptVersion, prompt, response, stopwatch.ElapsedMilliseconds, cancellationToken);
        return new AiTextResponse(response, request.PromptVersion, options.Value.Model);
    }

    private async Task LogAsync(Guid questionId, string operationType, string promptVersion, string prompt, string response, long latencyMs, CancellationToken cancellationToken)
    {
        dbContext.AIInteractionLogs.Add(AIInteractionLog.Create(
            null,
            operationType,
            "Ollama",
            options.Value.Model,
            promptVersion,
            OllamaHashing.ComputeHash(prompt),
            $"question:{questionId}",
            response[..Math.Min(response.Length, 3000)],
            (int)latencyMs,
            true,
            clock.UtcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class OllamaCodeFeedbackService(
    OllamaApiClient client,
    ITrainingPlatformDbContext dbContext,
    IClock clock,
    IOptions<OllamaOptions> options) : ICodeFeedbackService
{
    public async Task<AiTextResponse> EvaluateAsync(CodeFeedbackRequest request, CancellationToken cancellationToken)
    {
        var prompt = PromptCatalog.BuildCodeFeedbackPrompt(request);
        var stopwatch = Stopwatch.StartNew();
        var response = await client.GenerateAsync(prompt, cancellationToken);
        stopwatch.Stop();

        await LogAsync(request.ChallengeId, "CodeFeedback", request.PromptVersion, prompt, response, stopwatch.ElapsedMilliseconds, cancellationToken);
        return new AiTextResponse(response, request.PromptVersion, options.Value.Model);
    }

    private async Task LogAsync(Guid challengeId, string operationType, string promptVersion, string prompt, string response, long latencyMs, CancellationToken cancellationToken)
    {
        dbContext.AIInteractionLogs.Add(AIInteractionLog.Create(
            null,
            operationType,
            "Ollama",
            options.Value.Model,
            promptVersion,
            OllamaHashing.ComputeHash(prompt),
            $"coding-challenge:{challengeId}",
            response[..Math.Min(response.Length, 3000)],
            (int)latencyMs,
            true,
            clock.UtcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class OllamaScenarioEvaluationService(
    OllamaApiClient client,
    ITrainingPlatformDbContext dbContext,
    IClock clock,
    IOptions<OllamaOptions> options) : IScenarioEvaluationService
{
    public async Task<AiTextResponse> EvaluateAsync(ScenarioEvaluationRequest request, CancellationToken cancellationToken)
    {
        var prompt = PromptCatalog.BuildScenarioFeedbackPrompt(request);
        var stopwatch = Stopwatch.StartNew();
        var response = await client.GenerateAsync(prompt, cancellationToken);
        stopwatch.Stop();

        await LogAsync(request.ScenarioChallengeId, "ScenarioEvaluation", request.PromptVersion, prompt, response, stopwatch.ElapsedMilliseconds, cancellationToken);
        return new AiTextResponse(response, request.PromptVersion, options.Value.Model);
    }

    private async Task LogAsync(Guid challengeId, string operationType, string promptVersion, string prompt, string response, long latencyMs, CancellationToken cancellationToken)
    {
        dbContext.AIInteractionLogs.Add(AIInteractionLog.Create(
            null,
            operationType,
            "Ollama",
            options.Value.Model,
            promptVersion,
            OllamaHashing.ComputeHash(prompt),
            $"scenario-challenge:{challengeId}",
            response[..Math.Min(response.Length, 3000)],
            (int)latencyMs,
            true,
            clock.UtcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal sealed record GeneratedQuestionPayload(
    string Prompt,
    string Explanation,
    List<string> AcceptedAnswers,
    List<GeneratedQuestionOptionPayload> Options);

internal sealed record GeneratedQuestionOptionPayload(string Text, bool IsCorrect);

internal static class OllamaHashing
{
    public static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
