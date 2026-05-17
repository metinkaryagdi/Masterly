using TrainingPlatform.Domain.Identity;

namespace TrainingPlatform.Application.Abstractions.Security;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
