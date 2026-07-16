using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrainingPlatform.Api.Common;
using TrainingPlatform.Api.RateLimiting;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Features.Questions;

namespace TrainingPlatform.Api.Controllers;

[Route("api/questions")]
public sealed class QuestionsController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher) : AuthenticatedControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<PracticeQuestionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PracticeQuestionDto>>> GetAll([FromQuery] Guid? topicId, CancellationToken cancellationToken)
    {
        var response = await queryDispatcher.Dispatch<GetQuestionsQuery, IReadOnlyCollection<PracticeQuestionDto>>(new GetQuestionsQuery(topicId), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PracticeQuestionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PracticeQuestionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await queryDispatcher.Dispatch<GetQuestionByIdQuery, PracticeQuestionDto>(new GetQuestionByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicies.AiQuestionGeneration)]
    [ProducesResponseType<GeneratedQuestionResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<GeneratedQuestionResult>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<GeneratedQuestionResult>> Generate(GenerateQuestionCommand command, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<GenerateQuestionCommand, GeneratedQuestionResult>(command, cancellationToken);
        return response.Persisted ? Ok(response) : UnprocessableEntity(response);
    }
}
