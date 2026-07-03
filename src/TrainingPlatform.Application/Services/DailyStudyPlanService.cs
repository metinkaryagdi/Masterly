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
        IReadOnlyCollection<UserAnswer> recentAnswers,
        DateTime studyDateUtc,
        DateTime generatedAtUtc)
    {
        var plan = DailyStudyPlan.Create(user.Id, studyDateUtc, generatedAtUtc);

        var progressLookup = progressEntries.ToDictionary(entry => entry.TopicId);
        var scheduleLookup = revisionSchedules.ToDictionary(entry => entry.TopicId);
        var questionsByTopic = questions.GroupBy(question => question.TopicId).ToDictionary(group => group.Key, group => group.ToList());
        var eligibleTopics = BuildTopicCandidates(topics, progressLookup, scheduleLookup, generatedAtUtc);
        var distribution = CalculateDistribution(user.Preferences.DailyQuestionTarget);

        // Questions the learner already answered correctly in the recent window
        // are deprioritised: they only reappear when a topic's pool of fresh
        // questions runs dry.
        var recentCorrectQuestionIds = recentAnswers
            .Where(answer => answer.WasCorrect)
            .Select(answer => answer.QuestionId)
            .ToHashSet();

        // Seeded per user + study date: regenerating the same day's plan picks
        // the same questions, while tomorrow's draw rotates through the pool.
        var random = new Random(user.Id.GetHashCode() ^ studyDateUtc.Date.GetHashCode());

        var selectedQuestionIds = new HashSet<Guid>();
        var sequence = 1;

        foreach (var category in distribution.Keys)
        {
            var quota = distribution[category];
            if (quota <= 0)
            {
                continue;
            }

            sequence = FillQuota(
                plan, eligibleTopics[category], questionsByTopic, recentCorrectQuestionIds,
                selectedQuestionIds, random, quota, sequence, generatedAtUtc);
        }

        // Categories with no eligible topics (or dry pools) leave the plan under
        // target; back-fill from every eligible topic so the daily question count
        // holds whenever the pool allows.
        var shortfall = user.Preferences.DailyQuestionTarget - selectedQuestionIds.Count;
        if (shortfall > 0)
        {
            var allCandidates = eligibleTopics.Values.SelectMany(candidates => candidates).ToList();
            sequence = FillQuota(
                plan, allCandidates, questionsByTopic, recentCorrectQuestionIds,
                selectedQuestionIds, random, shortfall, sequence, generatedAtUtc);
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

    /// <summary>
    /// Draws up to <paramref name="quota"/> questions from the given topics'
    /// pools, round-robin across topics so no single topic monopolises the plan.
    /// Fresh questions (not recently answered correctly) are exhausted before
    /// repeats are considered; within a topic, questions closest to the
    /// learner's difficulty band come first, shuffled among ties.
    /// </summary>
    private static int FillQuota(
        DailyStudyPlan plan,
        IReadOnlyList<TopicCandidate> topicCandidates,
        IReadOnlyDictionary<Guid, List<Question>> questionsByTopic,
        HashSet<Guid> recentCorrectQuestionIds,
        HashSet<Guid> selectedQuestionIds,
        Random random,
        int quota,
        int sequence,
        DateTime generatedAtUtc)
    {
        var queues = new List<(TopicCandidate Candidate, Queue<Question> Fresh, Queue<Question> Repeats)>();
        foreach (var candidate in topicCandidates)
        {
            if (!questionsByTopic.TryGetValue(candidate.Topic.Id, out var pool))
            {
                continue;
            }

            var targetDifficulty = TargetDifficulty(candidate.Progress);
            var ordered = pool
                .Where(question => !selectedQuestionIds.Contains(question.Id))
                .OrderBy(question => Math.Abs((int)question.Difficulty - (int)targetDifficulty))
                .ThenBy(_ => random.Next())
                .ToList();

            var fresh = new Queue<Question>(ordered.Where(question => !recentCorrectQuestionIds.Contains(question.Id)));
            var repeats = new Queue<Question>(ordered.Where(question => recentCorrectQuestionIds.Contains(question.Id)));
            if (fresh.Count > 0 || repeats.Count > 0)
            {
                queues.Add((candidate, fresh, repeats));
            }
        }

        foreach (var useRepeats in new[] { false, true })
        {
            while (quota > 0)
            {
                var progressed = false;
                foreach (var (candidate, fresh, repeats) in queues)
                {
                    if (quota == 0)
                    {
                        break;
                    }

                    var queue = useRepeats ? repeats : fresh;
                    while (queue.Count > 0)
                    {
                        var question = queue.Dequeue();
                        if (!selectedQuestionIds.Add(question.Id))
                        {
                            continue;
                        }

                        plan.AddItem(
                            StudyPlanItemType.Question,
                            question.Id,
                            question.TopicId,
                            candidate.Category.ToString().ToLowerInvariant(),
                            sequence++,
                            candidate.Priority,
                            generatedAtUtc);
                        quota--;
                        progressed = true;
                        break;
                    }
                }

                if (!progressed)
                {
                    break;
                }
            }

            if (quota == 0)
            {
                break;
            }
        }

        return sequence;
    }

    /// <summary>
    /// The difficulty band a learner should mostly see for a topic: fundamentals
    /// until mastery reaches 40, intermediate up to 70, advanced beyond that.
    /// </summary>
    private static TopicDifficulty TargetDifficulty(TopicProgress? progress) => (progress?.MasteryScore ?? 0) switch
    {
        < 40 => TopicDifficulty.Fundamental,
        < 70 => TopicDifficulty.Intermediate,
        _ => TopicDifficulty.Advanced,
    };

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
