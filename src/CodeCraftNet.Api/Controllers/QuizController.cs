using Microsoft.AspNetCore.Mvc;
using CodeCraftNet.Api.Common;
using CodeCraftNet.Application.Common.Cqrs;
using CodeCraftNet.Application.Features.Quiz;

namespace CodeCraftNet.Api.Controllers;

public sealed record SubmitAnswerRequest(Guid QuestionId, Guid? SelectedOptionId, string? SubmittedAnswer, int ResponseTimeSeconds, Guid? DailyStudyPlanId);

[Route("api/quiz")]
public sealed class QuizController(ICommandDispatcher commandDispatcher) : AuthenticatedControllerBase
{
    [HttpPost("answers")]
    [ProducesResponseType<SubmitAnswerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SubmitAnswerResponse>> SubmitAnswer(SubmitAnswerRequest request, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<SubmitAnswerCommand, SubmitAnswerResponse>(
            new SubmitAnswerCommand(CurrentUserId, request.QuestionId, request.SelectedOptionId, request.SubmittedAnswer, request.ResponseTimeSeconds, request.DailyStudyPlanId),
            cancellationToken);

        return Ok(response);
    }
}
