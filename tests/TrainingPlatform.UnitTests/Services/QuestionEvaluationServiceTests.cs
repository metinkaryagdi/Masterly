using TrainingPlatform.Application.Services;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Questions;

namespace TrainingPlatform.UnitTests.Services;

public sealed class QuestionEvaluationServiceTests
{
    private static readonly Guid TopicId = Guid.NewGuid();
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Question MultipleChoice()
        => Question.Create(
            TopicId,
            QuestionType.MultipleChoice,
            prompt: "Which LINQ operator projects?",
            explanation: "Select transforms each element.",
            TopicDifficulty.Fundamental,
            estimatedSolvingTimeSeconds: 60,
            minimumPassingScore: 100,
            tags: Array.Empty<string>(),
            acceptedAnswers: Array.Empty<string>(),
            options: new[] { ("Where", false), ("Select", true), ("GroupBy", false) },
            createdAtUtc: CreatedAt);

    private static Question ShortAnswer()
        => Question.Create(
            TopicId,
            QuestionType.ShortAnswer,
            prompt: "Async keyword?",
            explanation: "await",
            TopicDifficulty.Fundamental,
            estimatedSolvingTimeSeconds: 45,
            minimumPassingScore: 100,
            tags: Array.Empty<string>(),
            acceptedAnswers: new[] { "await" },
            options: Array.Empty<(string, bool)>(),
            createdAtUtc: CreatedAt);

    // Mirrors the real seeded short-answer questions, whose passing score is 70 —
    // low enough that a partial match counts as correct. This is the case the
    // Contains("") bug scored a blank answer as a pass.
    private static Question LenientShortAnswer()
        => Question.Create(
            TopicId,
            QuestionType.ShortAnswer,
            prompt: "Async keyword?",
            explanation: "await",
            TopicDifficulty.Fundamental,
            estimatedSolvingTimeSeconds: 45,
            minimumPassingScore: 70,
            tags: Array.Empty<string>(),
            acceptedAnswers: new[] { "await" },
            options: Array.Empty<(string, bool)>(),
            createdAtUtc: CreatedAt);

    private static Question Scenario()
        => Question.Create(
            TopicId,
            QuestionType.Scenario,
            prompt: "Why split read and write models?",
            explanation: "Different concerns.",
            TopicDifficulty.Advanced,
            estimatedSolvingTimeSeconds: 120,
            minimumPassingScore: 50,
            tags: Array.Empty<string>(),
            acceptedAnswers: new[] { "read", "write", "scalability" },
            options: Array.Empty<(string, bool)>(),
            createdAtUtc: CreatedAt);

    [Fact]
    public void MultipleChoice_selecting_the_correct_option_returns_full_score()
    {
        var question = MultipleChoice();
        var correctOptionId = question.Options.Single(option => option.IsCorrect).Id;
        var service = new QuestionEvaluationService();

        var result = service.Evaluate(question, submittedAnswer: null, selectedOptionId: correctOptionId, responseTimeSeconds: 30);

        Assert.True(result.WasCorrect);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void MultipleChoice_selecting_a_wrong_option_returns_zero()
    {
        var question = MultipleChoice();
        var wrongOptionId = question.Options.First(option => !option.IsCorrect).Id;
        var service = new QuestionEvaluationService();

        var result = service.Evaluate(question, submittedAnswer: null, selectedOptionId: wrongOptionId, responseTimeSeconds: 30);

        Assert.False(result.WasCorrect);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void ShortAnswer_is_case_insensitive_and_trim_friendly()
    {
        var service = new QuestionEvaluationService();
        var question = ShortAnswer();

        var result = service.Evaluate(question, submittedAnswer: "  AWAIT  ", selectedOptionId: null, responseTimeSeconds: 20);

        Assert.True(result.WasCorrect);
        Assert.Equal(100, result.Score);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ShortAnswer_rejects_blank_submissions(string? submitted)
    {
        var service = new QuestionEvaluationService();
        var question = LenientShortAnswer();

        var result = service.Evaluate(question, submittedAnswer: submitted, selectedOptionId: null, responseTimeSeconds: 20);

        // Before the fix, "".Contains(accepted)/accepted.Contains("") scored this
        // as a 70-point partial match — a pass on a 70-threshold question.
        Assert.False(result.WasCorrect);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void ShortAnswer_rejects_a_one_character_fragment()
    {
        var service = new QuestionEvaluationService();
        var question = LenientShortAnswer(); // accepts "await"

        var result = service.Evaluate(question, submittedAnswer: "a", selectedOptionId: null, responseTimeSeconds: 20);

        // "await".Contains("a") is true, but a single character is below the
        // partial-match floor and must not count.
        Assert.False(result.WasCorrect);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void ShortAnswer_still_accepts_a_meaningful_partial_match()
    {
        var service = new QuestionEvaluationService();
        var question = LenientShortAnswer(); // accepts "await"

        // "awaited" contains "await" (5 chars, above the floor) → partial pass.
        var result = service.Evaluate(question, submittedAnswer: "awaited", selectedOptionId: null, responseTimeSeconds: 20);

        Assert.True(result.WasCorrect);
        Assert.Equal(70, result.Score);
    }

    [Fact]
    public void Scenario_scores_by_keyword_coverage()
    {
        var service = new QuestionEvaluationService();
        var question = Scenario();

        var result = service.Evaluate(
            question,
            submittedAnswer: "Splitting read paths from write paths improves scalability and allows independent optimization.",
            selectedOptionId: null,
            responseTimeSeconds: 60);

        Assert.True(result.WasCorrect);
        Assert.Equal(100, result.Score);
        Assert.Equal(1d, result.CoverageScore);
    }

    [Fact]
    public void Scenario_with_no_matching_keywords_fails_below_pass_threshold()
    {
        var service = new QuestionEvaluationService();
        var question = Scenario();

        var result = service.Evaluate(
            question,
            submittedAnswer: "I have no idea, honestly.",
            selectedOptionId: null,
            responseTimeSeconds: 60);

        Assert.False(result.WasCorrect);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void Speed_score_clamps_to_one_when_user_answers_faster_than_estimate()
    {
        var service = new QuestionEvaluationService();
        var question = MultipleChoice(); // estimated 60s
        var correctOptionId = question.Options.Single(option => option.IsCorrect).Id;

        var result = service.Evaluate(question, submittedAnswer: null, selectedOptionId: correctOptionId, responseTimeSeconds: 5);

        Assert.Equal(1d, result.SpeedScore);
    }
}
