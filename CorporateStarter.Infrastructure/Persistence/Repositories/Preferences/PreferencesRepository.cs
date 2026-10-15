using CorporateStarter.Application.Common.Interfaces.Repositories.Preferences;
using CorporateStarter.Core.Entities.Configuration;
using CorporateStarter.Shared.Dtos.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace CorporateStarter.Infrastructure.Persistence.Repositories.Preferences;

public sealed class PreferencesRepository(AppDbContext db) : IPreferencesRepository
{
    public async Task<GeneralPreferencesDto> GetAsync(CancellationToken ct) =>
        await db.Set<ApplicationPreferences>().AsNoTracking().Where(x => x.Id == 1)
            .Select(x => new GeneralPreferencesDto(x.Name, x.Revision)).SingleAsync(ct);

    public async Task<GeneralPreferencesDto?> UpdateAsync(UpdateGeneralPreferencesRequest request, CancellationToken ct)
    {
        var row = await db.Set<ApplicationPreferences>().SingleAsync(x => x.Id == 1, ct);
        if (row.Revision != request.Revision) return null;
        row.Name = request.Name.Trim();
        row.Revision = Guid.NewGuid();
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return null; }
        return new(row.Name, row.Revision);
    }

    // Explicit allowlist: never return the connection string, credentials, certificate paths or extra parameters.
    public ConnectionInfoDto GetConnections()
    {
        var connection = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());
        return new("PostgreSQL", connection.Host ?? "", connection.Port, connection.Database ?? "");
    }
}

