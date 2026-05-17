using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Common.Cqrs;

namespace TrainingPlatform.Application.Features.Analytics;

public sealed record TopicMasteryDto(Guid TopicId, string TopicName, int MasteryScore, double ForgettingRisk, double Accuracy);

public sealed record LearningTrendPointDto(DateTime DayUtc, double Accuracy, int Attempts);

public sealed record AnalyticsDashboardDto(
    IReadOnlyCollection<TopicMasteryDto> TopicMastery,
    IReadOnlyCollection<TopicMasteryDto> WeakAreas,
    IReadOnlyCollection<LearningTrendPointDto> LearningTrend,
    double AverageResponseTimeSeconds,
    int ConsistencyDays,
    double ChallengeSuccessRate);

public sealed record GetDashboardQuery(Guid UserId) : IQuery<AnalyticsDashboardDto>;

public sealed class GetDashboardQueryValidator : AbstractValidator<GetDashboardQuery>
{
    public GetDashboardQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
    }
}

public sealed class GetDashboardQueryHandler(ITrainingPlatformDbContext dbContext) : IQueryHandler<GetDashboardQuery, AnalyticsDashboardDto>
{
    public async Task<AnalyticsDashboardDto> Handle(GetDashboardQuery query, CancellationToken cancellationToken)
    {
        var topicData = await dbContext.TopicProgressEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == query.UserId)
            .Join(
                dbContext.Topics.AsNoTracking(),
                progress => progress.TopicId,
                topic => topic.Id,
                (progress, topic) => new { progress, topic })
            .GroupJoin(
                dbContext.RevisionSchedules.AsNoTracking().Where(entry => entry.UserId == query.UserId),
                item => item.progress.TopicId,
                schedule => schedule.TopicId,
                (item, schedules) => new { item.progress, item.topic, schedule = schedules.FirstOrDefault() })
            .Select(item => new TopicMasteryDto(
                item.topic.Id,
                item.topic.Name,
                item.progress.MasteryScore,
                item.schedule != null ? item.schedule.ForgettingRisk : 0d,
                item.progress.Accuracy))
            .ToListAsync(cancellationToken);

        var answers = await dbContext.UserAnswers
            .AsNoTracking()
            .Where(answer => answer.UserId == query.UserId)
            .ToListAsync(cancellationToken);

        var learningTrend = answers
            .GroupBy(answer => answer.CreatedAtUtc.Date)
            .OrderBy(group => group.Key)
            .Select(group => new LearningTrendPointDto(
                group.Key,
                group.Count(answer => answer.WasCorrect) / (double)group.Count(),
                group.Count()))
            .ToList();

        var consistencyDays = answers
            .Select(answer => answer.CreatedAtUtc.Date)
            .Distinct()
            .OrderByDescending(date => date)
            .TakeWhile((date, index) => date == DateTime.UtcNow.Date.AddDays(-index))
            .Count();

        var codingSubmissions = await dbContext.CodingSubmissions.AsNoTracking().Where(entry => entry.UserId == query.UserId && entry.Score.HasValue).ToListAsync(cancellationToken);
        var scenarioSubmissions = await dbContext.ScenarioSubmissions.AsNoTracking().Where(entry => entry.UserId == query.UserId && entry.Score.HasValue).ToListAsync(cancellationToken);
        var completedChallengeCount = codingSubmissions.Count + scenarioSubmissions.Count;
        var successfulChallengeCount = codingSubmissions.Count(entry => entry.Score >= 70) + scenarioSubmissions.Count(entry => entry.Score >= 70);

        return new AnalyticsDashboardDto(
            topicData,
            topicData.OrderBy(entry => entry.MasteryScore).ThenByDescending(entry => entry.ForgettingRisk).Take(5).ToList(),
            learningTrend,
            answers.Count == 0 ? 0d : answers.Average(answer => answer.ResponseTimeSeconds),
            consistencyDays,
            completedChallengeCount == 0 ? 0d : successfulChallengeCount / (double)completedChallengeCount);
    }
}
