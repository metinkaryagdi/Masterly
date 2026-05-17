namespace TrainingPlatform.Application.Features.Identity;

public sealed record AuthResponse(Guid UserId, string DisplayName, string Email, string AccessToken);
