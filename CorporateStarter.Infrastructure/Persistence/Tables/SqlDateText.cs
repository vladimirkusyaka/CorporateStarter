using System.Globalization;
namespace CorporateStarter.Infrastructure.Persistence.Tables;

public static class SqlDateText
{
    public static string Utc(string quotedColumn) => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "de" or "ru" => $"to_char({quotedColumn} AT TIME ZONE 'UTC', 'DD.MM.YYYY HH24:MI:SS')",
        _ => $"to_char({quotedColumn} AT TIME ZONE 'UTC', 'FMMM/FMDD/YYYY FMHH12:MI:SS AM')"
    };
}
