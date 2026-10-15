using CorporateStarter.Shared.Dtos.Configuration;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Preferences
{
    public interface IPreferencesRepository
    {
        Task<GeneralPreferencesDto> GetAsync(CancellationToken ct);
        Task<GeneralPreferencesDto?> UpdateAsync(UpdateGeneralPreferencesRequest request, CancellationToken ct);
        ConnectionInfoDto GetConnections();
    }
}

