using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;

namespace TrainingPlatform.Application.Features.Identity;

public sealed record UserPreferencesDto(
    int DailyQuestionTarget,
    int DailyStudyMinutes,
    int DailyCodingChallengeTarget,
    int DailyScenarioChallengeTarget,
    bool IncludeWeekends);

public sealed record GetUserPreferencesQuery(Guid UserId) : IQuery<UserPreferencesDto>;

public sealed class GetUserPreferencesQueryValidator : AbstractValidator<GetUserPreferencesQuery>
{
    public GetUserPreferencesQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
    }
}

public sealed class GetUserPreferencesQueryHandler(ITrainingPlatformDbContext dbContext)
    : IQueryHandler<GetUserPreferencesQuery, UserPreferencesDto>
{
    public async Task<UserPreferencesDto> Handle(GetUserPreferencesQuery query, CancellationToken cancellationToken)
    {
        var preferences = await dbContext.UserPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.UserId == query.UserId, cancellationToken)
            ?? throw new NotFoundException("No preferences exist for the current user.");

        return new UserPreferencesDto(
            preferences.DailyQuestionTarget,
            preferences.DailyStudyMinutes,
            preferences.DailyCodingChallengeTarget,
            preferences.DailyScenarioChallengeTarget,
            preferences.IncludeWeekends);
    }
}

public sealed record UpdateUserPreferencesCommand(
    Guid UserId,
    int DailyQuestionTarget,
    int DailyStudyMinutes,
    int DailyCodingChallengeTarget,
    int DailyScenarioChallengeTarget,
    bool IncludeWeekends) : ICommand<UserPreferencesDto>;

public sealed class UpdateUserPreferencesCommandValidator : AbstractValidator<UpdateUserPreferencesCommand>
{
    public UpdateUserPreferencesCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.DailyQuestionTarget).InclusiveBetween(1, 100);
        RuleFor(command => command.DailyStudyMinutes).InclusiveBetween(5, 480);
        RuleFor(command => command.DailyCodingChallengeTarget).InclusiveBetween(0, 10);
        RuleFor(command => command.DailyScenarioChallengeTarget).InclusiveBetween(0, 10);
    }
}

public sealed class UpdateUserPreferencesCommandHandler(ITrainingPlatformDbContext dbContext, IClock clock)
    : ICommandHandler<UpdateUserPreferencesCommand, UserPreferencesDto>
{
    public async Task<UserPreferencesDto> Handle(UpdateUserPreferencesCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(entry => entry.Preferences)
            .SingleOrDefaultAsync(entry => entry.Id == command.UserId, cancellationToken)
            ?? throw new NotFoundException("The current user no longer exists.");

        user.UpdatePreferences(
            command.DailyQuestionTarget,
            command.DailyStudyMinutes,
            command.DailyCodingChallengeTarget,
            command.DailyScenarioChallengeTarget,
            command.IncludeWeekends,
            clock.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserPreferencesDto(
            user.Preferences.DailyQuestionTarget,
            user.Preferences.DailyStudyMinutes,
            user.Preferences.DailyCodingChallengeTarget,
            user.Preferences.DailyScenarioChallengeTarget,
            user.Preferences.IncludeWeekends);
    }
}
