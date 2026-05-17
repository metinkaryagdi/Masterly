using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Security;
using TrainingPlatform.Application.Common.Cqrs;

namespace TrainingPlatform.Application.Features.Identity;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    ITrainingPlatformDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(entry => entry.Email == normalizedEmail, cancellationToken)
                   ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!passwordHasher.Verify(user.PasswordHash, command.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return new AuthResponse(user.Id, user.DisplayName, user.Email, jwtTokenGenerator.Generate(user));
    }
}
