using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TrainingPlatform.Application.Abstractions.AI;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Domain.Common.Enumerations;
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

        // BaseAddress/Timeout are configured once at registration
        // (AddHttpClient<OllamaApiClient>). Mutating them here threw
        // InvalidOperationException on the second request of a reused client,
        // which broke the generation retry loop — never set them per-call.
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
        var typeRule = request.QuestionType switch
        {
            QuestionType.MultipleChoice =>
                "Tam 3 veya 4 \"options\" üret; SADECE birinin \"isCorrect\" değeri true olsun; \"acceptedAnswers\" boş dizi olsun.",
            QuestionType.ShortAnswer =>
                "\"options\" boş dizi olsun; \"acceptedAnswers\" 1-4 kısa kabul edilebilir cevap içersin (Türkçe ya da teknik terim).",
            _ =>
                "\"options\" boş dizi olsun; \"acceptedAnswers\" cevap metninde geçmesi beklenen 3-5 Türkçe anahtar kelime içersin."
        };

        return $$"""
                 .NET backend eğitimi için TÜRKÇE bir sınav sorusu üret.
                 SADECE geçerli JSON döndür; markdown, açıklama ya da kod bloğu ekleme.
                 JSON anahtarları tam olarak şunlar olmalı: prompt, explanation, acceptedAnswers, options.
                 - "prompt": soru metni (Türkçe).
                 - "explanation": cevabın neden doğru olduğunu anlatan kısa açıklama (Türkçe).
                 - "options": her biri {"text": string, "isCorrect": bool} olan nesneler dizisi.
                 - "acceptedAnswers": string dizisi.
                 Soru tipi kuralı: {{typeRule}}
                 Konu: {{request.TopicName}}
                 Soru tipi: {{request.QuestionType}}
                 Zorluk: {{request.Difficulty}}
                 Etiketler: {{string.Join(", ", request.Tags)}}
                 Tüm metinler Türkçe olmalı. Sürüm: {{request.PromptVersion}}
                 """;
    }

    public static string BuildAnswerEvaluationPrompt(AnswerEvaluationRequest request)
    {
        return $$"""
                 Gönderilen cevabı bir koç gibi açıkla.
                 Puan verme. Kısa ve öz tut. Geri bildirimi tamamen Türkçe yaz.
                 Sürüm: {{request.PromptVersion}}
                 Soru: {{request.Prompt}}
                 Gönderilen cevap: {{request.SubmittedAnswer}}
                 """;
    }

    public static string BuildCodeFeedbackPrompt(CodeFeedbackRequest request)
    {
        return $$"""
                 Bu backend kod gönderimini incele ve yalnızca geri bildirim ver.
                 Kesin doğru/yanlış hükmü verme. Kısa ve öz tut.
                 Geri bildirimi tamamen Türkçe yaz; kod terimleri ve tip adları İngilizce kalabilir.
                 Sürüm: {{request.PromptVersion}}
                 Görev: {{request.ChallengeDescription}}
                 Gönderilen kod:
                 {{request.SubmittedCode}}
                 """;
    }

    public static string BuildScenarioFeedbackPrompt(ScenarioEvaluationRequest request)
    {
        return $$"""
                 Bu mimari senaryo yanıtını incele ve yalnızca geri bildirim ver.
                 Ödünleşimleri (trade-off) ve gözden kaçan noktaları vurgula.
                 Geri bildirimi tamamen Türkçe yaz; teknik terimler İngilizce kalabilir.
                 Sürüm: {{request.PromptVersion}}
                 Senaryo: {{request.Scenario}}
                 Yanıt:
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

        var json = ExtractJsonObject(response);
        var payload = JsonSerializer.Deserialize<GeneratedQuestionPayload>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                      ?? throw new InvalidOperationException("Ollama returned an invalid question payload.");

        return new GeneratedQuestion(
            payload.Prompt ?? string.Empty,
            payload.Explanation ?? string.Empty,
            payload.AcceptedAnswers ?? [],
            (payload.Options ?? []).Select(option => (option.Text, option.IsCorrect)).ToList());
    }

    /// <summary>
    /// Local models often wrap JSON in prose or ```json fences. Pull out the
    /// first balanced top-level object so the payload still deserializes.
    /// </summary>
    private static string ExtractJsonObject(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException("Ollama returned an empty response.");
        }

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("Ollama response did not contain a JSON object.");
        }

        return response.Substring(start, end - start + 1);
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
