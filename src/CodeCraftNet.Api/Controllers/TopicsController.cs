using Microsoft.AspNetCore.Mvc;
using CodeCraftNet.Api.Common;
using CodeCraftNet.Application.Common.Cqrs;
using CodeCraftNet.Application.Features.Topics;

namespace CodeCraftNet.Api.Controllers;

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

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TopicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TopicDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await queryDispatcher.Dispatch<GetTopicByIdQuery, TopicDto>(new GetTopicByIdQuery(id), cancellationToken);
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
