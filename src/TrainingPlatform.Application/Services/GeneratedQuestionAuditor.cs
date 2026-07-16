using System.Text.RegularExpressions;
using TrainingPlatform.Application.Abstractions.AI;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Application.Services;

/// <summary>
/// Outcome of auditing a single AI-generated question. <see cref="Passed"/> is
/// true only when there are no blocking issues; <see cref="Issues"/> always
/// carries the human-readable reasons a candidate was rejected (in Turkish, so
/// they can surface straight to an admin UI).
/// </summary>
public sealed record GeneratedQuestionAuditReport(bool Passed, IReadOnlyList<string> Issues)
{
    public static GeneratedQuestionAuditReport Ok() => new(true, Array.Empty<string>());

    public static GeneratedQuestionAuditReport Rejected(IReadOnlyList<string> issues) => new(false, issues);
}

/// <summary>
/// Deterministically verifies ("denetler") a question produced by the AI model
/// before it is allowed into the pool. The check is pure and side-effect free so
/// it is fully unit-testable without a running model: it validates structure,
/// type-consistency, language, and duplication.
/// </summary>
public interface IGeneratedQuestionAuditor
{
    GeneratedQuestionAuditReport Audit(
        QuestionType questionType,
        GeneratedQuestion question,
        IReadOnlyCollection<string> existingPrompts);
}

public sealed class GeneratedQuestionAuditor : IGeneratedQuestionAuditor
{
    private const int MinPromptLength = 12;
    private const int MaxPromptLength = 4000;
    private const int MinExplanationLength = 10;
    private const int MinMultipleChoiceOptions = 2;
    private const int MaxMultipleChoiceOptions = 6;

    // Whole-word English function words. The model is instructed to answer in
    // Turkish; hitting several of these is a strong signal it slipped back into
    // English, so the candidate is rejected rather than silently stored.
    private static readonly string[] EnglishStopWords =
    [
        "the", "which", "what", "does", "with", "your", "when", "where",
        "that", "this", "these", "should", "would", "answer", "question",
        "into", "each", "from", "will", "must", "have", "and", "for", "how"
    ];

    public GeneratedQuestionAuditReport Audit(
        QuestionType questionType,
        GeneratedQuestion question,
        IReadOnlyCollection<string> existingPrompts)
    {
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(existingPrompts);

        var issues = new List<string>();

        var prompt = (question.Prompt ?? string.Empty).Trim();
        if (prompt.Length < MinPromptLength)
        {
            issues.Add($"Soru metni çok kısa (en az {MinPromptLength} karakter olmalı).");
        }
        else if (prompt.Length > MaxPromptLength)
        {
            issues.Add($"Soru metni çok uzun (en fazla {MaxPromptLength} karakter olmalı).");
        }

        if ((question.Explanation ?? string.Empty).Trim().Length < MinExplanationLength)
        {
            issues.Add("Açıklama eksik ya da çok kısa.");
        }

        if (prompt.Length >= MinPromptLength && LooksEnglish(prompt))
        {
            issues.Add("Soru Türkçe değil gibi görünüyor; model İngilizce üretmiş olabilir.");
        }

        if (IsDuplicate(prompt, existingPrompts))
        {
            issues.Add("Bu konuda aynı soru zaten mevcut (yinelenen soru).");
        }

        switch (questionType)
        {
            case QuestionType.MultipleChoice:
                AuditMultipleChoice(question, issues);
                break;
            case QuestionType.ShortAnswer:
            case QuestionType.Scenario:
                AuditOpenEnded(question, issues);
                break;
            default:
                issues.Add($"Bilinmeyen soru tipi: {questionType}.");
                break;
        }

        return issues.Count == 0
            ? GeneratedQuestionAuditReport.Ok()
            : GeneratedQuestionAuditReport.Rejected(issues);
    }

    private static void AuditMultipleChoice(GeneratedQuestion question, List<string> issues)
    {
        var options = question.Options ?? Array.Empty<(string Text, bool IsCorrect)>();
        if (options.Count < MinMultipleChoiceOptions)
        {
            issues.Add($"Çoktan seçmeli soru en az {MinMultipleChoiceOptions} seçenek içermeli.");
        }

        if (options.Count > MaxMultipleChoiceOptions)
        {
            issues.Add($"Çoktan seçmeli soru en fazla {MaxMultipleChoiceOptions} seçenek içermeli.");
        }

        if (options.Any(option => string.IsNullOrWhiteSpace(option.Text)))
        {
            issues.Add("Boş seçenek metni var.");
        }

        var correctCount = options.Count(option => option.IsCorrect);
        if (correctCount != 1)
        {
            issues.Add($"Çoktan seçmeli soruda tam olarak bir doğru seçenek olmalı (bulunan: {correctCount}).");
        }

        var distinctTexts = options
            .Select(option => Normalize(option.Text))
            .Where(text => text.Length > 0)
            .Distinct()
            .Count();
        if (distinctTexts != options.Count && options.Count > 0)
        {
            issues.Add("Seçenekler benzersiz değil (yinelenen seçenek metni).");
        }
    }

    private static void AuditOpenEnded(GeneratedQuestion question, List<string> issues)
    {
        var accepted = (question.AcceptedAnswers ?? Array.Empty<string>())
            .Where(answer => !string.IsNullOrWhiteSpace(answer))
            .ToList();
        if (accepted.Count == 0)
        {
            issues.Add("Kısa cevap/senaryo sorusu en az bir kabul edilen cevap ya da anahtar kelime tanımlamalı.");
        }

        if ((question.Options?.Count ?? 0) > 0)
        {
            issues.Add("Açık uçlu soru seçenek içermemeli.");
        }
    }

    private static bool IsDuplicate(string prompt, IReadOnlyCollection<string> existingPrompts)
    {
        var normalized = Normalize(prompt);
        if (normalized.Length == 0)
        {
            return false;
        }

        return existingPrompts.Any(existing => Normalize(existing) == normalized);
    }

    private static bool LooksEnglish(string prompt)
    {
        var words = Regex.Matches(prompt.ToLowerInvariant(), "[a-zçğıöşü]+")
            .Select(match => match.Value)
            .ToHashSet();

        // Two or more English function words is far beyond what a Turkish prompt
        // borrowing a single loanword would trip.
        return EnglishStopWords.Count(word => words.Contains(word)) >= 2;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value.Trim().ToLowerInvariant(), "\\s+", " ");
    }
}
