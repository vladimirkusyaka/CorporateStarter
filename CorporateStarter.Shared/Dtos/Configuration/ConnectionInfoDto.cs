namespace CorporateStarter.Shared.Dtos.Configuration;

public sealed record ConnectionInfoDto(string Provider, string Server, int Port, string Database);
