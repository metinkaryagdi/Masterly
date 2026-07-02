using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;
using TrainingPlatform.Domain.Identity;
using TrainingPlatform.Domain.Progress;

namespace TrainingPlatform.Application.Features.Identity;

public sealed record SelfAssessmentInput(Guid TopicId, SelfAssessmentLevel Level);

public sealed record CompleteOnboardingCommand(
    Guid UserId,
    int DailyQuestionTarget,
    int DailyStudyMinutes,
    int DailyCodingChallengeTarget,
    int DailyScenarioChallengeTarget,
    bool IncludeWeekends,
    IReadOnlyList<string> Goals,
    IReadOnlyList<SelfAssessmentInput> Assessments) : ICommand<UserPreferencesDto>;

public sealed class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
{
    public CompleteOnboardingCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.DailyQuestionTarget).InclusiveBetween(1, 100);
        RuleFor(command => command.DailyStudyMinutes).InclusiveBetween(5, 480);
        RuleFor(command => command.DailyCodingChallengeTarget).InclusiveBetween(0, 10);
        RuleFor(command => command.DailyScenarioChallengeTarget).InclusiveBetween(0, 10);
        RuleFor(command => command.Goals).NotNull();
        RuleFor(command => command.Goals.Count).LessThanOrEqualTo(20)
            .WithMessage("At most 20 goals are supported.");
        RuleForEach(command => command.Goals).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Assessments).NotNull();
        RuleForEach(command => command.Assessments).ChildRules(assessment =>
        {
            assessment.RuleFor(item => item.TopicId).NotEmpty();
            assessment.RuleFor(item => item.Level).IsInEnum();
        });
        RuleFor(command => command.Assessments)
            .Must(assessments => assessments.Select(item => item.TopicId).Distinct().Count() == assessments.Count)
            .When(command => command.Assessments is not null)
            .WithMessage("Each topic may only appear once in the self-assessment.");
    }
}

public sealed class CompleteOnboardingCommandHandler(ITrainingPlatformDbContext dbContext, IClock clock)
    : ICommandHandler<CompleteOnboardingCommand, UserPreferencesDto>
{
    public async Task<UserPreferencesDto> Handle(CompleteOnboardingCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(entry => entry.Preferences)
            .SingleOrDefaultAsync(entry => entry.Id == command.UserId, cancellationToken)
            ?? throw new NotFoundException("The current user no longer exists.");

        var now = clock.UtcNow;

        user.UpdatePreferences(
            command.DailyQuestionTarget,
            command.DailyStudyMinutes,
            command.DailyCodingChallengeTarget,
            command.DailyScenarioChallengeTarget,
            command.IncludeWeekends,
            command.Goals,
            now);

        if (command.Assessments.Count > 0)
        {
            var requestedTopicIds = command.Assessments.Select(item => item.TopicId).ToHashSet();

            var existingTopicIds = await dbContext.Topics
                .Where(topic => requestedTopicIds.Contains(topic.Id))
                .Select(topic => topic.Id)
                .ToListAsync(cancellationToken);

            var existingTopicLookup = existingTopicIds.ToHashSet();
            var missingTopicIds = requestedTopicIds.Where(id => !existingTopicLookup.Contains(id)).ToList();
            if (missingTopicIds.Count > 0)
            {
                throw new NotFoundException(
                    $"Unknown topic id(s) in self-assessment: {string.Join(", ", missingTopicIds)}");
            }

            var existingAssessments = await dbContext.TopicSelfAssessments
                .Where(assessment => assessment.UserId == command.UserId
                                      && requestedTopicIds.Contains(assessment.TopicId))
                .ToListAsync(cancellationToken);

            var existingProgressTopicIds = await dbContext.TopicProgressEntries
                .Where(progress => progress.UserId == command.UserId
                                    && requestedTopicIds.Contains(progress.TopicId))
                .Select(progress => progress.TopicId)
                .ToListAsync(cancellationToken);
            var progressLookup = existingProgressTopicIds.ToHashSet();

            foreach (var input in command.Assessments)
            {
                var existing = existingAssessments.FirstOrDefault(item => item.TopicId == input.TopicId);
                if (existing is null)
                {
                    var assessment = TopicSelfAssessment.Create(command.UserId, input.TopicId, input.Level, now);
                    dbContext.TopicSelfAssessments.Add(assessment);
                }
                else
                {
                    existing.Update(input.Level, now);
                }

                if (!progressLookup.Contains(input.TopicId))
                {
                    var seed = TopicSelfAssessmentSeed(input.Level);
                    var progress = TopicProgress.CreateSeeded(command.UserId, input.TopicId, seed, now);
                    dbContext.TopicProgressEntries.Add(progress);
                    progressLookup.Add(input.TopicId);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserPreferencesDto(
            user.Preferences.DailyQuestionTarget,
            user.Preferences.DailyStudyMinutes,
            user.Preferences.DailyCodingChallengeTarget,
            user.Preferences.DailyScenarioChallengeTarget,
            user.Preferences.IncludeWeekends,
            user.Preferences.Goals.ToList());
    }

    private static int TopicSelfAssessmentSeed(SelfAssessmentLevel level) => level switch
    {
        SelfAssessmentLevel.Novice => 20,
        SelfAssessmentLevel.Familiar => 45,
        SelfAssessmentLevel.Strong => 70,
        _ => 25,
    };
}
