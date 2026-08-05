using CodeCraftNet.Domain.Identity;

namespace CodeCraftNet.Application.Abstractions.Security;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
