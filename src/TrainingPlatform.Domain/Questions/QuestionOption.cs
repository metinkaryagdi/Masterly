using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Questions;

public sealed class QuestionOption : Entity
{
    private QuestionOption()
    {
    }

    private QuestionOption(Guid id, Guid questionId, string text, bool isCorrect, int order, DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        QuestionId = questionId;
        Text = text;
        IsCorrect = isCorrect;
        Order = order;
    }

    public Guid QuestionId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public bool IsCorrect { get; private set; }

    public int Order { get; private set; }

    public static QuestionOption Create(Guid questionId, string text, bool isCorrect, int order, DateTime createdAtUtc)
    {
        return new QuestionOption(Guid.NewGuid(), questionId, text.Trim(), isCorrect, order, createdAtUtc);
    }
}
