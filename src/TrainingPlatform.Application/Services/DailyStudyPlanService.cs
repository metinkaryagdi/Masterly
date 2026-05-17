using TrainingPlatform.Application.Common.Models;
using TrainingPlatform.Domain.Challenges;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Identity;
using TrainingPlatform.Domain.Progress;
using TrainingPlatform.Domain.Questions;
using TrainingPlatform.Domain.Topics;

namespace TrainingPlatform.Application.Services;

public sealed class DailyStudyPlanService : IDailyStudyPlanService
{
    public DailyStudyPlan BuildPlan(
        User user,
        IReadOnlyCollection<Topic> topics,
        IReadOnlyCollection<Question> questions,
        IReadOnlyCollection<CodingChallenge> codingChallenges,
        IReadOnlyCollection<ScenarioChallenge> scenarioChallenges,
        IReadOnlyCollection<TopicProgress> progressEntries,
        IReadOnlyCollection<RevisionSchedule> revisionSchedules,
        DateTime studyDateUtc,
        DateTime generatedAtUtc)
    {
        var plan = DailyStudyPlan.Create(user.Id, studyDateUtc, generatedAtUtc);

        var progressLookup = progressEntries.ToDictionary(entry => entry.TopicId);
        var scheduleLookup = revisionSchedules.ToDictionary(entry => entry.TopicId);
        var questionsByTopic = questions.GroupBy(question => question.TopicId).ToDictionary(group => group.Key, group => group.ToList());
        var eligibleTopics = BuildTopicCandidates(topics, progressLookup, scheduleLookup, generatedAtUtc);
        var distribution = CalculateDistribution(user.Preferences.DailyQuestionTarget);
        var selectedQuestionIds = new HashSet<Guid>();
        var sequence = 1;

        foreach (var category in distribution.Keys)
        {
            var quota = distribution[category];
            if (quota <= 0)
            {
                continue;
            }

            foreach (var topic in eligibleTopics[category])
            {
                if (!questionsByTopic.TryGetValue(topic.Topic.Id, out var topicQuestions))
                {
                    continue;
                }

                foreach (var question in topicQuestions.OrderBy(question => question.Difficulty).ThenBy(question => question.CreatedAtUtc))
                {
                    if (!selectedQuestionIds.Add(question.Id))
                    {
                        continue;
                    }

                    plan.AddItem(StudyPlanItemType.Question, question.Id, question.TopicId, category.ToString().ToLowerInvariant(), sequence++, topic.Priority, generatedAtUtc);

                    if (--quota == 0)
                    {
                        break;
                    }
                }

                if (quota == 0)
                {
                    break;
                }
            }
        }

        var orderedTopicsForChallenges = eligibleTopics
            .SelectMany(pair => pair.Value)
            .OrderByDescending(candidate => candidate.Priority)
            .ThenBy(candidate => candidate.Progress?.MasteryScore ?? 0)
            .Select(candidate => candidate.Topic.Id)
            .Distinct()
            .ToList();

        var codingChallenge = codingChallenges
            .Where(challenge => challenge.IsActive)
            .OrderBy(challenge => orderedTopicsForChallenges.IndexOf(challenge.TopicId) is var index && index >= 0 ? index : int.MaxValue)
            .ThenBy(challenge => challenge.Difficulty)
            .FirstOrDefault();

        if (codingChallenge is not null)
        {
            plan.AddItem(StudyPlanItemType.CodingChallenge, codingChallenge.Id, codingChallenge.TopicId, "challenge", sequence++, 1d, generatedAtUtc);
        }

        var scenarioChallenge = scenarioChallenges
            .Where(challenge => challenge.IsActive)
            .OrderBy(challenge => orderedTopicsForChallenges.IndexOf(challenge.TopicId) is var index && index >= 0 ? index : int.MaxValue)
            .ThenBy(challenge => challenge.Difficulty)
            .FirstOrDefault();

        if (scenarioChallenge is not null)
        {
            plan.AddItem(StudyPlanItemType.ScenarioChallenge, scenarioChallenge.Id, scenarioChallenge.TopicId, "challenge", sequence, 1d, generatedAtUtc);
        }

        return plan;
    }

    private static Dictionary<TopicCategory, int> CalculateDistribution(int totalQuestions)
    {
        var weighted = new Dictionary<TopicCategory, double>
        {
            [TopicCategory.Weak] = totalQuestions * 0.4d,
            [TopicCategory.Recent] = totalQuestions * 0.3d,
            [TopicCategory.Strong] = totalQuestions * 0.2d,
            [TopicCategory.New] = totalQuestions * 0.1d
        };

        var distribution = weighted.ToDictionary(pair => pair.Key, pair => (int)Math.Floor(pair.Value));
        var assigned = distribution.Values.Sum();
        var remaining = totalQuestions - assigned;

        foreach (var category in weighted.OrderByDescending(pair => pair.Value - Math.Floor(pair.Value)).Select(pair => pair.Key))
        {
            if (remaining == 0)
            {
                break;
            }

            distribution[category]++;
            remaining--;
        }

        return distribution;
    }

    private static Dictionary<TopicCategory, List<TopicCandidate>> BuildTopicCandidates(
        IReadOnlyCollection<Topic> topics,
        IReadOnlyDictionary<Guid, TopicProgress> progressLookup,
        IReadOnlyDictionary<Guid, RevisionSchedule> scheduleLookup,
        DateTime nowUtc)
    {
        var masteredTopicIds = progressLookup.Values.Where(progress => progress.MasteryScore >= 70).Select(progress => progress.TopicId).ToHashSet();

        var candidates = topics
            .Where(topic => topic.Dependencies.All(dependency => masteredTopicIds.Contains(dependency.DependsOnTopicId)) || topic.Dependencies.Count == 0)
            .Select(topic =>
            {
                progressLookup.TryGetValue(topic.Id, out var progress);
                scheduleLookup.TryGetValue(topic.Id, out var schedule);

                var category = DetermineCategory(progress, schedule, nowUtc);
                var priority = schedule?.PriorityScore
                    ?? (progress is null ? 0.5d : Math.Clamp(1d - (progress.MasteryScore / 100d), 0.1d, 0.9d));

                return new TopicCandidate(topic, progress, schedule, category, priority);
            })
            .ToList();

        return Enum.GetValues<TopicCategory>()
            .ToDictionary(
                category => category,
                category => candidates
                    .Where(candidate => candidate.Category == category)
                    .OrderByDescending(candidate => candidate.Priority)
                    .ThenBy(candidate => candidate.Progress?.MasteryScore ?? 0)
                    .ThenBy(candidate => candidate.Topic.Difficulty)
                    .ToList());
    }

    private static TopicCategory DetermineCategory(TopicProgress? progress, RevisionSchedule? schedule, DateTime nowUtc)
    {
        if (progress is null)
        {
            return TopicCategory.New;
        }

        if (progress.MasteryScore < 60 || (schedule?.ForgettingRisk ?? 0d) >= 0.6d)
        {
            return TopicCategory.Weak;
        }

        if (progress.LastActivityAtUtc.HasValue && (nowUtc - progress.LastActivityAtUtc.Value).TotalDays <= 7)
        {
            return TopicCategory.Recent;
        }

        return progress.MasteryScore >= 80 && (schedule?.ForgettingRisk ?? 0.2d) < 0.35d
            ? TopicCategory.Strong
            : TopicCategory.Recent;
    }

    private sealed record TopicCandidate(
        Topic Topic,
        TopicProgress? Progress,
        RevisionSchedule? Schedule,
        TopicCategory Category,
        double Priority);
}
