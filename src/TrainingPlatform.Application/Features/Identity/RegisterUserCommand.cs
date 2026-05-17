using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Security;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Common.Exceptions;
using TrainingPlatform.Domain.Identity;

namespace TrainingPlatform.Application.Features.Identity;

public sealed record RegisterUserCommand(string Email, string DisplayName, string Password) : ICommand<AuthResponse>;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.DisplayName).NotEmpty().MinimumLength(3).MaximumLength(120);
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public sealed class RegisterUserCommandHandler(
    ITrainingPlatformDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IClock clock) : ICommandHandler<RegisterUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var exists = await dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            throw new ConflictException("A user with the same email already exists.");
        }

        var user = User.Create(normalizedEmail, command.DisplayName, passwordHasher.Hash(command.Password), clock.UtcNow);
        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.Id, user.DisplayName, user.Email, jwtTokenGenerator.Generate(user));
    }
}
