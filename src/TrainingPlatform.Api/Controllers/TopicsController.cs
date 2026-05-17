using Microsoft.AspNetCore.Mvc;
using TrainingPlatform.Api.Common;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Features.Topics;

namespace TrainingPlatform.Api.Controllers;

[Route("api/topics")]
public sealed class TopicsController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher) : AuthenticatedControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<TopicDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TopicDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await queryDispatcher.Dispatch<GetTopicsQuery, IReadOnlyCollection<TopicDto>>(new GetTopicsQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<TopicDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TopicDto>> Create(CreateTopicCommand command, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<CreateTopicCommand, TopicDto>(command, cancellationToken);
        return Ok(response);
    }
}
