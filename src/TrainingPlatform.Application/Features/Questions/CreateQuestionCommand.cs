using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Questions;

namespace TrainingPlatform.Application.Features.Questions;

public sealed record CreateQuestionOptionRequest(string Text, bool IsCorrect);

public sealed record CreateQuestionCommand(
    Guid TopicId,
    QuestionType QuestionType,
    string Prompt,
    string Explanation,
    TopicDifficulty Difficulty,
    int EstimatedSolvingTimeSeconds,
    int MinimumPassingScore,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<string> AcceptedAnswers,
    IReadOnlyCollection<CreateQuestionOptionRequest> Options) : ICommand<QuestionDto>;

public sealed class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
{
    public CreateQuestionCommandValidator()
    {
        RuleFor(command => command.TopicId).NotEmpty();
        RuleFor(command => command.Prompt).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.Explanation).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.EstimatedSolvingTimeSeconds).InclusiveBetween(15, 1800);
        RuleFor(command => command.MinimumPassingScore).InclusiveBetween(1, 100);

        RuleFor(command => command)
            .Must(command => command.QuestionType != QuestionType.MultipleChoice || command.Options.Count >= 2)
            .WithMessage("Multiple choice questions must have at least two options.");

        RuleFor(command => command)
            .Must(command => command.QuestionType != QuestionType.MultipleChoice || command.Options.Count(option => option.IsCorrect) == 1)
            .WithMessage("Multiple choice questions must have exactly one correct option.");

        RuleFor(command => command)
            .Must(command => command.QuestionType == QuestionType.MultipleChoice || command.AcceptedAnswers.Count > 0)
            .WithMessage("Short answer and scenario questions must define accepted answers or keywords.");
    }
}

public sealed class CreateQuestionCommandHandler(
    ITrainingPlatformDbContext dbContext,
    IClock clock) : ICommandHandler<CreateQuestionCommand, QuestionDto>
{
    public async Task<QuestionDto> Handle(CreateQuestionCommand command, CancellationToken cancellationToken)
    {
        var topicExists = await dbContext.Topics.AnyAsync(topic => topic.Id == command.TopicId, cancellationToken);
        if (!topicExists)
        {
            throw new NotFoundException("The requested topic was not found.");
        }

        var question = Question.Create(
            command.TopicId,
            command.QuestionType,
            command.Prompt,
            command.Explanation,
            command.Difficulty,
            command.EstimatedSolvingTimeSeconds,
            command.MinimumPassingScore,
            command.Tags,
            command.AcceptedAnswers,
            command.Options.Select(option => (option.Text, option.IsCorrect)),
            clock.UtcNow);

        await dbContext.Questions.AddAsync(question, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(question);
    }

    private static QuestionDto Map(Question question)
    {
        return new QuestionDto(
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
            question.Options.OrderBy(option => option.Order).Select(option => new QuestionOptionDto(option.Id, option.Text, option.IsCorrect, option.Order)).ToList());
    }
}
