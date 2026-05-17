using Microsoft.AspNetCore.Mvc;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Features.Identity;

namespace TrainingPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ICommandDispatcher commandDispatcher) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<RegisterUserCommand, AuthResponse>(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var response = await commandDispatcher.Dispatch<LoginCommand, AuthResponse>(command, cancellationToken);
        return Ok(response);
    }
}
