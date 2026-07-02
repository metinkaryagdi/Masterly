using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Domain.Progress;

public sealed class TopicProgress : Entity
{
    private TopicProgress()
    {
    }

    private TopicProgress(Guid id, Guid userId, Guid topicId, DateTime createdAtUtc)
        : base(id, createdAtUtc)
    {
        UserId = userId;
        TopicId = topicId;
        MasteryScore = 25;
    }

    public Guid UserId { get; private set; }

    public Guid TopicId { get; private set; }

    public int TotalAttemptCount { get; private set; }

    public int CorrectAttemptCount { get; private set; }

    public double AverageResponseTimeSeconds { get; private set; }

    public int CurrentCorrectStreak { get; private set; }

    public int LongestCorrectStreak { get; private set; }

    public int MasteryScore { get; private set; }

    public double ConsistencyScore { get; private set; }

    public DateTime? LastActivityAtUtc { get; private set; }

    public int CodingChallengeAttempts { get; private set; }

    public int CodingChallengeSuccesses { get; private set; }

    public int ScenarioChallengeAttempts { get; private set; }

    public int ScenarioChallengeSuccesses { get; private set; }

    public double Accuracy => TotalAttemptCount == 0 ? 0d : (double)CorrectAttemptCount / TotalAttemptCount;

    public static TopicProgress Create(Guid userId, Guid topicId, DateTime createdAtUtc)
    {
        return new TopicProgress(Guid.NewGuid(), userId, topicId, createdAtUtc);
    }

    public static TopicProgress CreateSeeded(Guid userId, Guid topicId, int masterySeed, DateTime createdAtUtc)
    {
        var progress = new TopicProgress(Guid.NewGuid(), userId, topicId, createdAtUtc);
        progress.MasteryScore = Math.Clamp(masterySeed, 0, 100);
        return progress;
    }

    public void ApplyTheoryAttempt(bool wasCorrect, int responseTimeSeconds, int masteryScore, double consistencyScore, DateTime updatedAtUtc)
    {
        TotalAttemptCount++;
        if (wasCorrect)
        {
            CorrectAttemptCount++;
            CurrentCorrectStreak++;
            LongestCorrectStreak = Math.Max(LongestCorrectStreak, CurrentCorrectStreak);
        }
        else
        {
            CurrentCorrectStreak = 0;
        }

        AverageResponseTimeSeconds = TotalAttemptCount == 1
            ? responseTimeSeconds
            : ((AverageResponseTimeSeconds * (TotalAttemptCount - 1)) + responseTimeSeconds) / TotalAttemptCount;

        MasteryScore = masteryScore;
        ConsistencyScore = consistencyScore;
        LastActivityAtUtc = updatedAtUtc;
        Touch(updatedAtUtc);
    }

    public void ApplyCodingChallengeResult(bool succeeded, DateTime updatedAtUtc)
    {
        CodingChallengeAttempts++;
        if (succeeded)
        {
            CodingChallengeSuccesses++;
        }

        LastActivityAtUtc = updatedAtUtc;
        Touch(updatedAtUtc);
    }

    public void ApplyScenarioChallengeResult(bool succeeded, DateTime updatedAtUtc)
    {
        ScenarioChallengeAttempts++;
        if (succeeded)
        {
            ScenarioChallengeSuccesses++;
        }

        LastActivityAtUtc = updatedAtUtc;
        Touch(updatedAtUtc);
    }
}
