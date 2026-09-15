namespace CorporateStarter.Api.Infrastructure.Auth
{
    public sealed class AuthOperationContentionException : Exception
    {
        public AuthOperationContentionException(Exception innerException)
            : base(
                "Auth operation could not complete due to concurrent changes.",
                innerException)
        {
        }
    }
}
