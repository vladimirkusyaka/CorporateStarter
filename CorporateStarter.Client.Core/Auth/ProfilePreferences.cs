using System.Globalization;
using CorporateStarter.Shared.Dtos.Auth;
namespace CorporateStarter.Client.Core.Auth;

public sealed class ProfilePreferences
{
    public string Theme { get; private set; } = "System";
    public int PageSize { get; private set; } = 20;
    public string TimeZone { get; private set; } = "UTC";
    private TimeZoneInfo _deviceZone = TimeZoneInfo.Utc;
    public string ZoneLabel => TimeZone == "System" ? _deviceZone.Id : "UTC";
    public event Action? Changed;
    public void Apply(ProfileSettingsDto value)
    {
        if (!value.IsValid()) throw new InvalidDataException("Invalid profile settings.");
        Theme = value.Theme; PageSize = value.PageSize; TimeZone = value.TimeZone; Changed?.Invoke();
    }
    public void SetDeviceZone(string id)
    {
        try { _deviceZone = TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { _deviceZone = TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { _deviceZone = TimeZoneInfo.Utc; }
        Changed?.Invoke();
    }
    public string Format(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc),
        TimeZone == "System" ? _deviceZone : TimeZoneInfo.Utc).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
}
