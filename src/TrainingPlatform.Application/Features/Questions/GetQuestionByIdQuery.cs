using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;

namespace TrainingPlatform.Application.Features.Questions;

public sealed record GetQuestionByIdQuery(Guid QuestionId) : IQuery<PracticeQuestionDto>;

public sealed class GetQuestionByIdQueryValidator : AbstractValidator<GetQuestionByIdQuery>
{
    public GetQuestionByIdQueryValidator()
    {
        RuleFor(query => query.QuestionId).NotEmpty();
    }
}

public sealed class GetQuestionByIdQueryHandler(ITrainingPlatformDbContext dbContext) : IQueryHandler<GetQuestionByIdQuery, PracticeQuestionDto>
{
    public async Task<PracticeQuestionDto> Handle(GetQuestionByIdQuery query, CancellationToken cancellationToken)
    {
        var question = await dbContext.Questions
            .AsNoTracking()
            .Include(entry => entry.Options)
            .SingleOrDefaultAsync(entry => entry.Id == query.QuestionId, cancellationToken)
            ?? throw new NotFoundException("The requested question was not found.");

        // Read path: withhold the answer key (IsCorrect / AcceptedAnswers /
        // Explanation). The correct answer is only revealed post-submission.
        return new PracticeQuestionDto(
            question.Id,
            question.TopicId,
            question.QuestionType,
            question.Prompt,
            question.Difficulty,
            question.EstimatedSolvingTimeSeconds,
            question.MinimumPassingScore,
            question.Tags,
            question.Options.OrderBy(option => option.Order).Select(option => new PracticeQuestionOptionDto(option.Id, option.Text, option.Order)).ToList());
    }
}
