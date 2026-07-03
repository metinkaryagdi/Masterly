using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;

namespace TrainingPlatform.Application.Features.StudyPlans;

public sealed record GetDailyStudyPlanQuery(Guid UserId, DateTime? StudyDateUtc = null) : IQuery<DailyStudyPlanDto>;

public sealed class GetDailyStudyPlanQueryValidator : AbstractValidator<GetDailyStudyPlanQuery>
{
    public GetDailyStudyPlanQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
    }
}

public sealed class GetDailyStudyPlanQueryHandler(ITrainingPlatformDbContext dbContext) : IQueryHandler<GetDailyStudyPlanQuery, DailyStudyPlanDto>
{
    public async Task<DailyStudyPlanDto> Handle(GetDailyStudyPlanQuery query, CancellationToken cancellationToken)
    {
        var studyDateUtc = DateTime.SpecifyKind((query.StudyDateUtc ?? DateTime.UtcNow).Date, DateTimeKind.Utc);
        var plan = await dbContext.DailyStudyPlans
            .AsNoTracking()
            .Include(entry => entry.Items)
            .SingleOrDefaultAsync(entry => entry.UserId == query.UserId && entry.StudyDateUtc == studyDateUtc, cancellationToken)
            ?? throw new NotFoundException("No daily study plan exists for the requested date.");

        return await StudyPlanMapper.MapAsync(plan, dbContext, cancellationToken);
    }
}
