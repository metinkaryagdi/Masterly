using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Common.Cqrs;

namespace TrainingPlatform.Application.Features.Questions;

public sealed record GetQuestionsQuery(Guid? TopicId = null) : IQuery<IReadOnlyCollection<QuestionDto>>;

public sealed class GetQuestionsQueryValidator : AbstractValidator<GetQuestionsQuery>
{
}

public sealed class GetQuestionsQueryHandler(ITrainingPlatformDbContext dbContext) : IQueryHandler<GetQuestionsQuery, IReadOnlyCollection<QuestionDto>>
{
    public async Task<IReadOnlyCollection<QuestionDto>> Handle(GetQuestionsQuery query, CancellationToken cancellationToken)
    {
        var questions = dbContext.Questions
            .AsNoTracking()
            .Include(question => question.Options)
            .AsQueryable();

        if (query.TopicId.HasValue)
        {
            questions = questions.Where(question => question.TopicId == query.TopicId.Value);
        }

        return await questions
            .OrderBy(question => question.CreatedAtUtc)
            .Select(question => new QuestionDto(
                question.Id,
                question.TopicId,
                question.QuestionType,
                question.Prompt,
                question.Explanation,
                question.Difficulty,
                question.EstimatedSolvingTimeSeconds,
                question.MinimumPassingScore,
                question.Tags,
                question.AcceptedAnswers,
                question.Options.OrderBy(option => option.Order).Select(option => new QuestionOptionDto(option.Id, option.Text, option.IsCorrect, option.Order)).ToList()))
            .ToListAsync(cancellationToken);
    }
}
