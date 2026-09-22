using System.Net.Http;

namespace CorporateStarter.Client.Abstractions.Api;

/// <summary>
/// Sends authenticated business API requests without exposing access tokens.
/// Implementations must restrict destinations and must not refresh or retry.
/// </summary>
public interface IClientApiTransport
{
    Task<ClientApiResponse> SendAsync(
        HttpMethod method,
        string relativePath,
        Guid expectedUserId,
        string? jsonBody = null,
        CancellationToken cancellationToken = default);
}
