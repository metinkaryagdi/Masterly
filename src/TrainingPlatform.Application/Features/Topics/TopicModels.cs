using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Application.Features.Topics;

public sealed record TopicSampleItemDto(
    Guid Id,
    string Type,
    string Title,
    TopicDifficulty Difficulty,
    int Minutes);

public sealed record TopicDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    TopicDifficulty Difficulty,
    double DecayRate,
    IReadOnlyCollection<Guid> DependencyIds,
    int QuestionCount,
    int CodingChallengeCount,
    int ScenarioCount,
    IReadOnlyCollection<TopicSampleItemDto> SampleQuestions);

/// <summary>
/// Per-topic content stats surfaced on the topic catalogue: how many items the
/// pool holds and a handful of sample items the UI can launch directly.
/// </summary>
public sealed record TopicContentSummary(
    int QuestionCount,
    int CodingChallengeCount,
    int ScenarioCount,
    IReadOnlyCollection<TopicSampleItemDto> SampleQuestions)
{
    public static readonly TopicContentSummary Empty = new(0, 0, 0, []);

    private const int MaxSampleQuestions = 4;

    public static async Task<Dictionary<Guid, TopicContentSummary>> LoadAsync(
        ITrainingPlatformDbContext dbContext,
        IReadOnlyCollection<Guid> topicIds,
        CancellationToken cancellationToken)
    {
        var questions = await dbContext.Questions
            .AsNoTracking()
            .Where(question => topicIds.Contains(question.TopicId))
            .Select(question => new { question.TopicId, question.Id, question.Prompt, question.Difficulty, question.EstimatedSolvingTimeSeconds })
            .ToListAsync(cancellationToken);

        var codingChallenges = await dbContext.CodingChallenges
            .AsNoTracking()
            .Where(challenge => topicIds.Contains(challenge.TopicId))
            .Select(challenge => new { challenge.TopicId, challenge.Id, challenge.Title, challenge.Difficulty, challenge.EstimatedMinutes })
            .ToListAsync(cancellationToken);

        var scenarioChallenges = await dbContext.ScenarioChallenges
            .AsNoTracking()
            .Where(challenge => topicIds.Contains(challenge.TopicId))
            .Select(challenge => new { challenge.TopicId, challenge.Id, challenge.Title, challenge.Difficulty, challenge.EstimatedMinutes })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, TopicContentSummary>();
        foreach (var topicId in topicIds)
        {
            var topicQuestions = questions.Where(question => question.TopicId == topicId).ToList();
            var topicCoding = codingChallenges.Where(challenge => challenge.TopicId == topicId).ToList();
            var topicScenarios = scenarioChallenges.Where(challenge => challenge.TopicId == topicId).ToList();

            var samples = topicQuestions
                .OrderBy(question => question.Difficulty)
                .ThenBy(question => question.Id)
                .Take(MaxSampleQuestions)
                .Select(question => new TopicSampleItemDto(
                    question.Id,
                    "Question",
                    question.Prompt,
                    question.Difficulty,
                    Math.Max(1, (int)Math.Ceiling(question.EstimatedSolvingTimeSeconds / 60d))))
                .ToList();

            samples.AddRange(topicCoding
                .OrderBy(challenge => challenge.Difficulty)
                .Take(1)
                .Select(challenge => new TopicSampleItemDto(challenge.Id, "CodingChallenge", challenge.Title, challenge.Difficulty, challenge.EstimatedMinutes)));

            samples.AddRange(topicScenarios
                .OrderBy(challenge => challenge.Difficulty)
                .Take(1)
                .Select(challenge => new TopicSampleItemDto(challenge.Id, "ScenarioChallenge", challenge.Title, challenge.Difficulty, challenge.EstimatedMinutes)));

            result[topicId] = new TopicContentSummary(
                topicQuestions.Count,
                topicCoding.Count,
                topicScenarios.Count,
                samples);
        }

        return result;
    }
}
