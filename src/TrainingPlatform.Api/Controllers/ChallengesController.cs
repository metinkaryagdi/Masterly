using Microsoft.AspNetCore.Mvc;
using TrainingPlatform.Api.Common;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Features.Challenges;

namespace TrainingPlatform.Api.Controllers;

public sealed record SubmitCodingChallengeRequest(Guid CodingChallengeId, Guid? DailyStudyPlanId, string SubmittedCode, string Notes);

public sealed record SubmitScenarioChallengeRequest(Guid ScenarioChallengeId, Guid? DailyStudyPlanId, string ResponseText);

[Route("api/challenges")]
public sealed class ChallengesController(ICommandDispatcher commandDispatcher) : AuthenticatedControllerBase
{
    [HttpPost("coding")]
    [ProducesResponseType<CodingChallengeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CodingChallengeDto>> CreateCodingChallenge(CreateCodingChallengeCommand command, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<CreateCodingChallengeCommand, CodingChallengeDto>(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("scenario")]
    [ProducesResponseType<ScenarioChallengeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ScenarioChallengeDto>> CreateScenarioChallenge(CreateScenarioChallengeCommand command, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<CreateScenarioChallengeCommand, ScenarioChallengeDto>(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("coding/submissions")]
    [ProducesResponseType<SubmissionDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SubmissionDto>> SubmitCodingChallenge(SubmitCodingChallengeRequest request, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<SubmitCodingSubmissionCommand, SubmissionDto>(
            new SubmitCodingSubmissionCommand(CurrentUserId, request.CodingChallengeId, request.DailyStudyPlanId, request.SubmittedCode, request.Notes),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("scenario/submissions")]
    [ProducesResponseType<SubmissionDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SubmissionDto>> SubmitScenarioChallenge(SubmitScenarioChallengeRequest request, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<SubmitScenarioSubmissionCommand, SubmissionDto>(
            new SubmitScenarioSubmissionCommand(CurrentUserId, request.ScenarioChallengeId, request.DailyStudyPlanId, request.ResponseText),
            cancellationToken);

        return Ok(response);
    }
}
