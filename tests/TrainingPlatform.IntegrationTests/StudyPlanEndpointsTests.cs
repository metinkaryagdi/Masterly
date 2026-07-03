using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TrainingPlatform.Application.Features.StudyPlans;
using TrainingPlatform.Application.Features.Topics;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Progress;
using TrainingPlatform.Infrastructure.Persistence;

namespace TrainingPlatform.IntegrationTests;

public sealed class StudyPlanEndpointsTests(TrainingPlatformApiFactory factory) : IClassFixture<TrainingPlatformApiFactory>
{
    private async Task<HttpClient> OnboardedClientAsync(int dailyQuestionTarget = 8)
    {
        var client = await ApiFlows.RegisteredClientAsync(factory);
        var topics = await client.GetFromJsonAsync<List<TopicDto>>("/api/topics", ApiFlows.Json);
        Assert.NotNull(topics);

        // Assess every topic as Strong (mastery 70) so dependency gates open and
        // every topic is eligible for plan selection.
        var response = await client.PostAsJsonAsync("/api/me/onboarding", new
        {
            dailyQuestionTarget,
            dailyStudyMinutes = 30,
            dailyCodingChallengeTarget = 1,
            dailyScenarioChallengeTarget = 1,
            includeWeekends = true,
            goals = new[] { "pool testing" },
            assessments = topics.Select(topic => new { topicId = topic.Id, level = "Strong" }).ToArray(),
        });
        response.EnsureSuccessStatusCode();

        return client;
    }

    private static List<DailyStudyPlanItemDto> QuestionItems(DailyStudyPlanDto plan)
        => plan.Items.Where(item => item.ItemType == StudyPlanItemType.Question).ToList();

    [Fact]
    public async Task Generated_plan_draws_the_daily_target_from_the_question_pool()
    {
        var client = await OnboardedClientAsync(dailyQuestionTarget: 8);

        var response = await client.PostAsJsonAsync("/api/study-plans/generate", new { });
        response.EnsureSuccessStatusCode();
        var plan = await response.Content.ReadFromJsonAsync<DailyStudyPlanDto>(ApiFlows.Json);
        Assert.NotNull(plan);

        var questions = QuestionItems(plan);
        Assert.Equal(8, questions.Count);
        Assert.Equal(8, questions.Select(item => item.ReferenceId).Distinct().Count());
        // The pool spans every topic; a full-size draw should never come from a single topic.
        Assert.True(questions.Select(item => item.TopicId).Distinct().Count() > 1);
    }

    [Fact]
    public async Task Next_day_plan_rotates_out_questions_answered_correctly()
    {
        var client = await OnboardedClientAsync(dailyQuestionTarget: 6);

        var firstResponse = await client.PostAsJsonAsync("/api/study-plans/generate", new { });
        firstResponse.EnsureSuccessStatusCode();
        var firstPlan = await firstResponse.Content.ReadFromJsonAsync<DailyStudyPlanDto>(ApiFlows.Json);
        Assert.NotNull(firstPlan);
        var answeredQuestionId = QuestionItems(firstPlan).First().ReferenceId;

        // Record a correct answer directly; the quiz endpoint's evaluation
        // mechanics are covered elsewhere — this test cares about rotation.
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TrainingPlatformDbContext>();
            dbContext.UserAnswers.Add(UserAnswer.Create(
                firstPlan.UserId, answeredQuestionId, firstPlan.Id,
                "correct", wasCorrect: true, score: 100, responseTimeSeconds: 20, "ok", DateTime.UtcNow));
            await dbContext.SaveChangesAsync();
        }

        var secondResponse = await client.PostAsJsonAsync("/api/study-plans/generate", new
        {
            studyDateUtc = DateTime.UtcNow.AddDays(1),
        });
        secondResponse.EnsureSuccessStatusCode();
        var secondPlan = await secondResponse.Content.ReadFromJsonAsync<DailyStudyPlanDto>(ApiFlows.Json);
        Assert.NotNull(secondPlan);

        var secondQuestions = QuestionItems(secondPlan);
        Assert.Equal(6, secondQuestions.Count);
        Assert.DoesNotContain(answeredQuestionId, secondQuestions.Select(item => item.ReferenceId));
    }

    [Fact]
    public async Task Regenerating_the_same_day_returns_the_same_plan()
    {
        var client = await OnboardedClientAsync();

        var first = await client.PostAsJsonAsync("/api/study-plans/generate", new { });
        first.EnsureSuccessStatusCode();
        var firstPlan = await first.Content.ReadFromJsonAsync<DailyStudyPlanDto>(ApiFlows.Json);

        var second = await client.PostAsJsonAsync("/api/study-plans/generate", new { });
        second.EnsureSuccessStatusCode();
        var secondPlan = await second.Content.ReadFromJsonAsync<DailyStudyPlanDto>(ApiFlows.Json);

        Assert.NotNull(firstPlan);
        Assert.NotNull(secondPlan);
        Assert.Equal(firstPlan.Id, secondPlan.Id);
        Assert.Equal(
            QuestionItems(firstPlan).Select(item => item.ReferenceId).ToList(),
            QuestionItems(secondPlan).Select(item => item.ReferenceId).ToList());
    }
}
