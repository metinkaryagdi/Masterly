using Microsoft.AspNetCore.Identity;
using TrainingPlatform.Application.Abstractions.Security;

namespace TrainingPlatform.Infrastructure.Auth;

public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<object> _passwordHasher = new();

    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(new object(), password);
    }

    public bool Verify(string hashedPassword, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(new object(), hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
