using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;
using TrainingPlatform.Application.Services;

namespace TrainingPlatform.Application.Features.StudyPlans;

public sealed record GenerateDailyStudyPlanCommand(Guid UserId, DateTime? StudyDateUtc = null) : ICommand<DailyStudyPlanDto>;

public sealed class GenerateDailyStudyPlanCommandValidator : AbstractValidator<GenerateDailyStudyPlanCommand>
{
    public GenerateDailyStudyPlanCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
    }
}

public sealed class GenerateDailyStudyPlanCommandHandler(
    ITrainingPlatformDbContext dbContext,
    IDailyStudyPlanService dailyStudyPlanService,
    IClock clock) : ICommandHandler<GenerateDailyStudyPlanCommand, DailyStudyPlanDto>
{
    public async Task<DailyStudyPlanDto> Handle(GenerateDailyStudyPlanCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(entry => entry.Preferences)
            .SingleOrDefaultAsync(entry => entry.Id == command.UserId, cancellationToken)
            ?? throw new NotFoundException("The requested user was not found.");

        var studyDateUtc = (command.StudyDateUtc ?? clock.UtcNow).Date;

        var existingPlan = await dbContext.DailyStudyPlans
            .Include(plan => plan.Items)
            .SingleOrDefaultAsync(plan => plan.UserId == command.UserId && plan.StudyDateUtc == studyDateUtc, cancellationToken);

        if (existingPlan is not null)
        {
            return await StudyPlanMapper.MapAsync(existingPlan, dbContext, cancellationToken);
        }

        var topics = await dbContext.Topics.Include(topic => topic.Dependencies).ToListAsync(cancellationToken);
        var questions = await dbContext.Questions.Include(question => question.Options).ToListAsync(cancellationToken);
        var codingChallenges = await dbContext.CodingChallenges.ToListAsync(cancellationToken);
        var scenarioChallenges = await dbContext.ScenarioChallenges.ToListAsync(cancellationToken);
        var progressEntries = await dbContext.TopicProgressEntries.Where(entry => entry.UserId == command.UserId).ToListAsync(cancellationToken);
        var revisionSchedules = await dbContext.RevisionSchedules.Where(entry => entry.UserId == command.UserId).ToListAsync(cancellationToken);

        // Questions answered correctly inside this window are held back from new
        // plans so the pool rotates instead of repeating what the learner just got right.
        var recentAnswerCutoffUtc = studyDateUtc.AddDays(-7);
        var recentAnswers = await dbContext.UserAnswers
            .Where(answer => answer.UserId == command.UserId && answer.CreatedAtUtc >= recentAnswerCutoffUtc)
            .ToListAsync(cancellationToken);

        var plan = dailyStudyPlanService.BuildPlan(
            user,
            topics,
            questions,
            codingChallenges,
            scenarioChallenges,
            progressEntries,
            revisionSchedules,
            recentAnswers,
            studyDateUtc,
            clock.UtcNow);

        await dbContext.DailyStudyPlans.AddAsync(plan, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await StudyPlanMapper.MapAsync(plan, dbContext, cancellationToken);
    }
}
