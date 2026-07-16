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

    [HttpGet("{id:guid}")]
    [ProducesResponseType<QuestionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await queryDispatcher.Dispatch<GetQuestionByIdQuery, QuestionDto>(new GetQuestionByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<QuestionDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<QuestionDto>> Create(CreateQuestionCommand command, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<CreateQuestionCommand, QuestionDto>(command, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Generates a fresh Turkish question with the AI model, audits it, and
    /// persists it only if the audit passes. Returns the audit report either way:
    /// 200 with the stored question, or 422 with the reasons it was rejected.
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType<GeneratedQuestionResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<GeneratedQuestionResult>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<GeneratedQuestionResult>> Generate(GenerateQuestionCommand command, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<GenerateQuestionCommand, GeneratedQuestionResult>(command, cancellationToken);
        return response.Persisted ? Ok(response) : UnprocessableEntity(response);
    }
}
