using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Common.Cqrs;

namespace TrainingPlatform.Application.Features.Questions;

public sealed record GetQuestionsQuery(Guid? TopicId = null) : IQuery<IReadOnlyCollection<PracticeQuestionDto>>;

public sealed class GetQuestionsQueryValidator : AbstractValidator<GetQuestionsQuery>
{
}

public sealed class GetQuestionsQueryHandler(ITrainingPlatformDbContext dbContext) : IQueryHandler<GetQuestionsQuery, IReadOnlyCollection<PracticeQuestionDto>>
{
    public async Task<IReadOnlyCollection<PracticeQuestionDto>> Handle(GetQuestionsQuery query, CancellationToken cancellationToken)
    {
        var questions = dbContext.Questions
            .AsNoTracking()
            .Include(question => question.Options)
            .AsQueryable();

        if (query.TopicId.HasValue)
        {
            questions = questions.Where(question => question.TopicId == query.TopicId.Value);
        }

        // Read path: the answer key (IsCorrect / AcceptedAnswers / Explanation)
        // must never be projected here — see PracticeQuestionDto.
        return await questions
            .OrderBy(question => question.CreatedAtUtc)
            .Select(question => new PracticeQuestionDto(
                question.Id,
                question.TopicId,
                question.QuestionType,
                question.Prompt,
                question.Difficulty,
                question.EstimatedSolvingTimeSeconds,
                question.MinimumPassingScore,
                question.Tags,
                question.Options.OrderBy(option => option.Order).Select(option => new PracticeQuestionOptionDto(option.Id, option.Text, option.Order)).ToList()))
            .ToListAsync(cancellationToken);
    }
}
