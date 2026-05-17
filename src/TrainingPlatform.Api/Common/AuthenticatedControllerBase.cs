using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TrainingPlatform.Api.Common;

[ApiController]
[Authorize]
public abstract class AuthenticatedControllerBase : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return value is not null && Guid.TryParse(value, out var userId)
                ? userId
                : throw new UnauthorizedAccessException("The current token does not contain a valid user identifier.");
        }
    }
}
