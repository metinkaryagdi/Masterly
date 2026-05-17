using Microsoft.AspNetCore.Mvc;
using TrainingPlatform.Api.Common;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Features.Questions;

namespace TrainingPlatform.Api.Controllers;

[Route("api/questions")]
public sealed class QuestionsController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher) : AuthenticatedControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<QuestionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<QuestionDto>>> GetAll([FromQuery] Guid? topicId, CancellationToken cancellationToken)
    {
        var response = await queryDispatcher.Dispatch<GetQuestionsQuery, IReadOnlyCollection<QuestionDto>>(new GetQuestionsQuery(topicId), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<QuestionDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<QuestionDto>> Create(CreateQuestionCommand command, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<CreateQuestionCommand, QuestionDto>(command, cancellationToken);
        return Ok(response);
    }
}
