using System.Collections.ObjectModel;
using CorporateStarter.Shared.Common;

namespace CorporateStarter.Client.Core.Tables;

/// <summary>Client-side state of a server-paged table. PageIndex is zero-based.</summary>
public sealed class TableQueryState<TColumn, TFilter>
    where TColumn : struct, Enum
    where TFilter : notnull
{
    private readonly Dictionary<TColumn, TFilter> _filters = [];
    private int _pageIndex;
    private int _pageSize;

    public TableQueryState(int pageSize = 20)
    {
        PageSize = pageSize;
        Filters = new ReadOnlyDictionary<TColumn, TFilter>(_filters);
    }

    public IReadOnlyDictionary<TColumn, TFilter> Filters { get; }
    public TColumn? SortColumn { get; private set; }
    public bool SortDescending { get; private set; }
    public int TotalCount { get; private set; }
    public int TotalPages => Math.Max(1, (int)(((long)TotalCount + PageSize - 1) / PageSize));

    public int PageIndex
    {
        get => _pageIndex;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (value == int.MaxValue) throw new ArgumentOutOfRangeException(nameof(value));
            _pageIndex = value;
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100);
            if (_pageSize == value) return;
            _pageSize = value;
            _pageIndex = 0;
        }
    }

    public void ApplyPage(int pageNumber, int totalCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        PageIndex = pageNumber - 1;
        TotalCount = totalCount;
    }

    public void ToggleSort(TColumn column)
    {
        SortDescending = SortColumn is { } current &&
            EqualityComparer<TColumn>.Default.Equals(current, column) && !SortDescending;
        SortColumn = column;
    }

    public void SetFilter(TColumn column, TFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        _filters[column] = filter;
    }

    public void RemoveFilter(TColumn column) => _filters.Remove(column);
    public void ClearFilters() => _filters.Clear();

    /// <summary>Discard query state for a column that is no longer available.</summary>
    public void RemoveColumn(TColumn column)
    {
        RemoveFilter(column);
        if (SortColumn is { } current && EqualityComparer<TColumn>.Default.Equals(current, column))
        {
            SortColumn = null;
            SortDescending = false;
        }
    }

    public TableRequest BuildRequest(
        Func<TColumn, string> fieldName,
        Func<TColumn, TFilter, TableFilter> serializeFilter)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(serializeFilter);
        return new TableRequest
        {
            PageNumber = PageIndex + 1,
            PageSize = PageSize,
            Sorts = SortColumn is { } column
                ? [new TableSort(fieldName(column), SortDescending ? "desc" : "asc")]
                : [],
            Filters = _filters.Select(pair => CopyFilter(serializeFilter(pair.Key, pair.Value))).ToList()
        };
    }

    private static TableFilter CopyFilter(TableFilter filter) => filter with
    {
        Value = filter.Value?.Clone(),
        Values = filter.Values?.Select(value => value.Clone()).ToArray()
    };
}
