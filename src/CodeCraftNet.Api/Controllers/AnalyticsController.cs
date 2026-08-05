using Microsoft.AspNetCore.Mvc;
using CodeCraftNet.Api.Common;
using CodeCraftNet.Application.Common.Cqrs;
using CodeCraftNet.Application.Features.Analytics;

namespace CodeCraftNet.Api.Controllers;

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
