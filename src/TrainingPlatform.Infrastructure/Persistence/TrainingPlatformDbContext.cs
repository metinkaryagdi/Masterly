using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Domain.Challenges;
using TrainingPlatform.Domain.Identity;
using TrainingPlatform.Domain.Progress;
using TrainingPlatform.Domain.Questions;
using TrainingPlatform.Domain.Topics;

namespace TrainingPlatform.Infrastructure.Persistence;

public sealed class TrainingPlatformDbContext(DbContextOptions<TrainingPlatformDbContext> options)
    : DbContext(options), ITrainingPlatformDbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DbSet<User> Users => Set<User>();

    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    public DbSet<SkillTarget> SkillTargets => Set<SkillTarget>();

    public DbSet<Topic> Topics => Set<Topic>();

    public DbSet<TopicDependency> TopicDependencies => Set<TopicDependency>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();

    public DbSet<CodingChallenge> CodingChallenges => Set<CodingChallenge>();

    public DbSet<ScenarioChallenge> ScenarioChallenges => Set<ScenarioChallenge>();

    public DbSet<UserAnswer> UserAnswers => Set<UserAnswer>();

    public DbSet<CodingSubmission> CodingSubmissions => Set<CodingSubmission>();

    public DbSet<ScenarioSubmission> ScenarioSubmissions => Set<ScenarioSubmission>();

    public DbSet<TopicProgress> TopicProgressEntries => Set<TopicProgress>();

    public DbSet<RevisionSchedule> RevisionSchedules => Set<RevisionSchedule>();

    public DbSet<DailyStudyPlan> DailyStudyPlans => Set<DailyStudyPlan>();

    public DbSet<DailyStudyPlanItem> DailyStudyPlanItems => Set<DailyStudyPlanItem>();

    public DbSet<MistakeLog> MistakeLogs => Set<MistakeLog>();

    public DbSet<AIInteractionLog> AIInteractionLogs => Set<AIInteractionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var stringListConverter = new ValueConverter<List<string>, string>(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => JsonSerializer.Deserialize<List<string>>(value, JsonOptions) ?? new List<string>());

        var stringListComparer = new ValueComparer<List<string>>(
            (left, right) => (left ?? new List<string>()).SequenceEqual(right ?? new List<string>()),
            value => value.Aggregate(0, (current, item) => HashCode.Combine(current, item.GetHashCode(StringComparison.Ordinal))),
            value => value.ToList());

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Email).HasMaxLength(320).IsRequired();
            entity.Property(item => item.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(item => item.PasswordHash).IsRequired();
            entity.HasIndex(item => item.Email).IsUnique();
            entity.HasOne(item => item.Preferences).WithOne().HasForeignKey<UserPreference>(item => item.UserId);
            entity.HasMany(item => item.SkillTargets).WithOne().HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.ToTable("user_preferences");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.UserId).IsUnique();
        });

        modelBuilder.Entity<SkillTarget>(entity =>
        {
            entity.ToTable("skill_targets");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.UserId, item.TopicId }).IsUnique();
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.ToTable("topics");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Slug).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(1000).IsRequired();
            entity.HasIndex(item => item.Slug).IsUnique();
            entity.HasMany(item => item.Dependencies).WithOne().HasForeignKey(item => item.TopicId);
        });

        modelBuilder.Entity<TopicDependency>(entity =>
        {
            entity.ToTable("topic_dependencies");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.TopicId, item.DependsOnTopicId }).IsUnique();
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("questions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Prompt).HasMaxLength(4000).IsRequired();
            entity.Property(item => item.Explanation).HasMaxLength(4000).IsRequired();
            entity.Property(item => item.Tags).HasConversion(stringListConverter);
            entity.Property(item => item.AcceptedAnswers).HasConversion(stringListConverter);
            entity.Property(item => item.Tags).Metadata.SetValueComparer(stringListComparer);
            entity.Property(item => item.AcceptedAnswers).Metadata.SetValueComparer(stringListComparer);
            entity.HasMany(item => item.Options).WithOne().HasForeignKey(item => item.QuestionId);
        });

        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.ToTable("question_options");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Text).HasMaxLength(1000).IsRequired();
        });

        modelBuilder.Entity<CodingChallenge>(entity =>
        {
            entity.ToTable("coding_challenges");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(4000).IsRequired();
            entity.Property(item => item.EvaluationCriteria).HasConversion(stringListConverter);
            entity.Property(item => item.EvaluationCriteria).Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<ScenarioChallenge>(entity =>
        {
            entity.ToTable("scenario_challenges");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Scenario).HasMaxLength(4000).IsRequired();
            entity.Property(item => item.EvaluationCriteria).HasConversion(stringListConverter);
            entity.Property(item => item.EvaluationCriteria).Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<UserAnswer>(entity =>
        {
            entity.ToTable("user_answers");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SubmittedAnswer).HasMaxLength(4000).IsRequired();
            entity.Property(item => item.EvaluationSummary).HasMaxLength(1000).IsRequired();
            entity.HasIndex(item => new { item.UserId, item.QuestionId, item.CreatedAtUtc });
        });

        modelBuilder.Entity<CodingSubmission>(entity =>
        {
            entity.ToTable("coding_submissions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SubmittedCode).HasColumnType("text").IsRequired();
            entity.Property(item => item.Notes).HasMaxLength(2000);
        });

        modelBuilder.Entity<ScenarioSubmission>(entity =>
        {
            entity.ToTable("scenario_submissions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ResponseText).HasColumnType("text").IsRequired();
        });

        modelBuilder.Entity<TopicProgress>(entity =>
        {
            entity.ToTable("topic_progress");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.UserId, item.TopicId }).IsUnique();
        });

        modelBuilder.Entity<RevisionSchedule>(entity =>
        {
            entity.ToTable("revision_schedules");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.UserId, item.TopicId }).IsUnique();
        });

        modelBuilder.Entity<DailyStudyPlan>(entity =>
        {
            entity.ToTable("daily_study_plans");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.UserId, item.StudyDateUtc }).IsUnique();
            entity.HasMany(item => item.Items).WithOne().HasForeignKey(item => item.DailyStudyPlanId);
        });

        modelBuilder.Entity<DailyStudyPlanItem>(entity =>
        {
            entity.ToTable("daily_study_plan_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SourceCategory).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<MistakeLog>(entity =>
        {
            entity.ToTable("mistake_logs");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FailureType).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Notes).HasMaxLength(1000).IsRequired();
        });

        modelBuilder.Entity<AIInteractionLog>(entity =>
        {
            entity.ToTable("ai_interaction_logs");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.OperationType).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Provider).HasMaxLength(50).IsRequired();
            entity.Property(item => item.Model).HasMaxLength(100).IsRequired();
            entity.Property(item => item.PromptVersion).HasMaxLength(100).IsRequired();
            entity.Property(item => item.PromptHash).HasMaxLength(128).IsRequired();
            entity.Property(item => item.RequestSummary).HasMaxLength(2000).IsRequired();
            entity.Property(item => item.ResponseSummary).HasMaxLength(4000).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
