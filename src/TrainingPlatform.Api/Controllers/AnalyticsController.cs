using Microsoft.AspNetCore.Mvc;
using TrainingPlatform.Api.Common;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Features.Analytics;

namespace TrainingPlatform.Api.Controllers;

[Route("api/analytics")]
public sealed class AnalyticsController(IQueryDispatcher queryDispatcher) : AuthenticatedControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType<AnalyticsDashboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalyticsDashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        var response = await queryDispatcher.Dispatch<GetDashboardQuery, AnalyticsDashboardDto>(new GetDashboardQuery(CurrentUserId), cancellationToken);
        return Ok(response);
    }
}
