namespace CorporateStarter.Client.UI.Components.Tables;

public sealed record TableColumn<TItem, TColumn>(TColumn Key, string Field, string Label,
    Func<TItem, string?> Text, bool IsStatus = false) where TColumn : struct, Enum;
