namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security
{
    public interface IPasswordHasher
    {
        string Hash(string password);

        bool Verify(string password, string passwordHash);
    }
}
