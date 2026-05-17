using Microsoft.AspNetCore.Mvc;
using TrainingPlatform.Api.Common;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Features.StudyPlans;

namespace TrainingPlatform.Api.Controllers;

public sealed record GenerateStudyPlanRequest(DateTime? StudyDateUtc);

[Route("api/study-plans")]
public sealed class StudyPlansController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher) : AuthenticatedControllerBase
{
    [HttpPost("generate")]
    [ProducesResponseType<DailyStudyPlanDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyStudyPlanDto>> Generate(GenerateStudyPlanRequest request, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<GenerateDailyStudyPlanCommand, DailyStudyPlanDto>(
            new GenerateDailyStudyPlanCommand(CurrentUserId, request.StudyDateUtc),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("today")]
    [ProducesResponseType<DailyStudyPlanDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyStudyPlanDto>> GetToday([FromQuery] DateTime? studyDateUtc, CancellationToken cancellationToken)
    {
        var response = await queryDispatcher.Dispatch<GetDailyStudyPlanQuery, DailyStudyPlanDto>(
            new GetDailyStudyPlanQuery(CurrentUserId, studyDateUtc),
            cancellationToken);

        return Ok(response);
    }
}
