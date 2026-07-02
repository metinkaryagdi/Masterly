using Microsoft.AspNetCore.Mvc;
using TrainingPlatform.Api.Common;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Features.Identity;
using TrainingPlatform.Domain.Identity;

namespace TrainingPlatform.Api.Controllers;

public sealed record UpdatePreferencesRequest(
    int DailyQuestionTarget,
    int DailyStudyMinutes,
    int DailyCodingChallengeTarget,
    int DailyScenarioChallengeTarget,
    bool IncludeWeekends,
    IReadOnlyList<string>? Goals);

public sealed record SelfAssessmentItem(Guid TopicId, SelfAssessmentLevel Level);

public sealed record CompleteOnboardingRequest(
    int DailyQuestionTarget,
    int DailyStudyMinutes,
    int DailyCodingChallengeTarget,
    int DailyScenarioChallengeTarget,
    bool IncludeWeekends,
    IReadOnlyList<string> Goals,
    IReadOnlyList<SelfAssessmentItem> Assessments);

[Route("api/me")]
public sealed class MeController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher) : AuthenticatedControllerBase
{
    [HttpGet("preferences")]
    [ProducesResponseType<UserPreferencesDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserPreferencesDto>> GetPreferences(CancellationToken cancellationToken)
    {
        var response = await queryDispatcher.Dispatch<GetUserPreferencesQuery, UserPreferencesDto>(
            new GetUserPreferencesQuery(CurrentUserId),
            cancellationToken);

        return Ok(response);
    }

    [HttpPut("preferences")]
    [ProducesResponseType<UserPreferencesDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserPreferencesDto>> UpdatePreferences(UpdatePreferencesRequest request, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<UpdateUserPreferencesCommand, UserPreferencesDto>(
            new UpdateUserPreferencesCommand(
                CurrentUserId,
                request.DailyQuestionTarget,
                request.DailyStudyMinutes,
                request.DailyCodingChallengeTarget,
                request.DailyScenarioChallengeTarget,
                request.IncludeWeekends,
                request.Goals),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("onboarding")]
    [ProducesResponseType<UserPreferencesDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserPreferencesDto>> CompleteOnboarding(CompleteOnboardingRequest request, CancellationToken cancellationToken)
    {
        var assessments = (request.Assessments ?? Array.Empty<SelfAssessmentItem>())
            .Select(item => new SelfAssessmentInput(item.TopicId, item.Level))
            .ToList();

        var response = await commandDispatcher.Dispatch<CompleteOnboardingCommand, UserPreferencesDto>(
            new CompleteOnboardingCommand(
                CurrentUserId,
                request.DailyQuestionTarget,
                request.DailyStudyMinutes,
                request.DailyCodingChallengeTarget,
                request.DailyScenarioChallengeTarget,
                request.IncludeWeekends,
                request.Goals ?? Array.Empty<string>(),
                assessments),
            cancellationToken);

        return Ok(response);
    }
}
