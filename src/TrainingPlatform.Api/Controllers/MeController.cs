using Microsoft.AspNetCore.Mvc;
using TrainingPlatform.Api.Common;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Features.Identity;

namespace TrainingPlatform.Api.Controllers;

public sealed record UpdatePreferencesRequest(
    int DailyQuestionTarget,
    int DailyStudyMinutes,
    int DailyCodingChallengeTarget,
    int DailyScenarioChallengeTarget,
    bool IncludeWeekends);

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
                request.IncludeWeekends),
            cancellationToken);

        return Ok(response);
    }
}
