using TrainingPlatform.Application.Abstractions.AI;
using TrainingPlatform.Application.Services;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.UnitTests.Services;

public sealed class GeneratedQuestionAuditorTests
{
    private static readonly string[] NoExistingPrompts = Array.Empty<string>();

    private static GeneratedQuestion ValidMultipleChoice()
        => new(
            Prompt: "Hangi LINQ operatörü her elemanı yeni bir biçime dönüştürür?",
            Explanation: "Select her girdi elemanını yeni bir şekle dönüştürür.",
            AcceptedAnswers: Array.Empty<string>(),
            Options: new[] { ("Where", false), ("Select", true), ("GroupBy", false) });

    private static GeneratedQuestion ValidShortAnswer()
        => new(
            Prompt: "C#'ta bir Task sonucunu asenkron beklemek için hangi anahtar kelime kullanılır?",
            Explanation: "await anahtar kelimesi görev tamamlandığında sürdürür.",
            AcceptedAnswers: new[] { "await" },
            Options: Array.Empty<(string, bool)>());

    [Fact]
    public void Valid_multiple_choice_passes_the_audit()
    {
        var report = new GeneratedQuestionAuditor()
            .Audit(QuestionType.MultipleChoice, ValidMultipleChoice(), NoExistingPrompts);

        Assert.True(report.Passed);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Valid_short_answer_passes_the_audit()
    {
        var report = new GeneratedQuestionAuditor()
            .Audit(QuestionType.ShortAnswer, ValidShortAnswer(), NoExistingPrompts);

        Assert.True(report.Passed);
    }

    [Fact]
    public void Multiple_choice_without_exactly_one_correct_option_is_rejected()
    {
        var question = ValidMultipleChoice() with
        {
            Options = new[] { ("Where", true), ("Select", true), ("GroupBy", false) }
        };

        var report = new GeneratedQuestionAuditor()
            .Audit(QuestionType.MultipleChoice, question, NoExistingPrompts);

        Assert.False(report.Passed);
        Assert.Contains(report.Issues, issue => issue.Contains("doğru seçenek"));
    }

    [Fact]
    public void Multiple_choice_with_a_single_option_is_rejected()
    {
        var question = ValidMultipleChoice() with { Options = new[] { ("Select", true) } };

        var report = new GeneratedQuestionAuditor()
            .Audit(QuestionType.MultipleChoice, question, NoExistingPrompts);

        Assert.False(report.Passed);
        Assert.Contains(report.Issues, issue => issue.Contains("en az"));
    }

    [Fact]
    public void Multiple_choice_with_duplicate_option_text_is_rejected()
    {
        var question = ValidMultipleChoice() with
        {
            Options = new[] { ("Select", true), ("select", false), ("GroupBy", false) }
        };

        var report = new GeneratedQuestionAuditor()
            .Audit(QuestionType.MultipleChoice, question, NoExistingPrompts);

        Assert.False(report.Passed);
        Assert.Contains(report.Issues, issue => issue.Contains("benzersiz"));
    }

    [Fact]
    public void Short_answer_without_accepted_answers_is_rejected()
    {
        var question = ValidShortAnswer() with { AcceptedAnswers = Array.Empty<string>() };

        var report = new GeneratedQuestionAuditor()
            .Audit(QuestionType.ShortAnswer, question, NoExistingPrompts);

        Assert.False(report.Passed);
        Assert.Contains(report.Issues, issue => issue.Contains("kabul edilen cevap"));
    }

    [Fact]
    public void Open_ended_question_carrying_options_is_rejected()
    {
        var question = ValidShortAnswer() with { Options = new[] { ("something", true) } };

        var report = new GeneratedQuestionAuditor()
            .Audit(QuestionType.ShortAnswer, question, NoExistingPrompts);

        Assert.False(report.Passed);
        Assert.Contains(report.Issues, issue => issue.Contains("seçenek içermemeli"));
    }

    [Fact]
    public void Too_short_prompt_is_rejected()
    {
        var question = ValidMultipleChoice() with { Prompt = "Kısa?" };

        var report = new GeneratedQuestionAuditor()
            .Audit(QuestionType.MultipleChoice, question, NoExistingPrompts);

        Assert.False(report.Passed);
        Assert.Contains(report.Issues, issue => issue.Contains("çok kısa"));
    }

    [Fact]
    public void English_prompt_is_flagged_as_not_turkish()
    {
        var question = ValidMultipleChoice() with
        {
            Prompt = "Which LINQ operator projects each element into a new form?"
        };

        var report = new GeneratedQuestionAuditor()
            .Audit(QuestionType.MultipleChoice, question, NoExistingPrompts);

        Assert.False(report.Passed);
        Assert.Contains(report.Issues, issue => issue.Contains("Türkçe değil"));
    }

    [Fact]
    public void Duplicate_prompt_against_existing_pool_is_rejected()
    {
        var question = ValidShortAnswer();
        var existing = new[] { "  c#'TA bir task sonucunu asenkron beklemek için hangi ANAHTAR kelime kullanılır?  " };

        var report = new GeneratedQuestionAuditor()
            .Audit(QuestionType.ShortAnswer, question, existing);

        Assert.False(report.Passed);
        Assert.Contains(report.Issues, issue => issue.Contains("yinelenen soru"));
    }
}
