namespace CodeCraftNet.Application.Abstractions.Security;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hashedPassword, string providedPassword);
}
