namespace CorporateStarter.Client.Core.Tables;

/// <summary>Selection within the displayed page, in its current row order.</summary>
public sealed class TableSelection<TKey> where TKey : notnull
{
    private readonly HashSet<TKey> _selected = [];
    private TKey _anchor = default!;
    private bool _hasAnchor;

    public IReadOnlySet<TKey> SelectedIds => _selected;

    public void Clear()
    {
        _selected.Clear();
        _anchor = default!;
        _hasAnchor = false;
    }

    public void RetainVisible(IEnumerable<TKey> visibleIds, bool selectFirstIfEmpty = false)
    {
        ArgumentNullException.ThrowIfNull(visibleIds);
        var ids = visibleIds.ToArray();
        _selected.IntersectWith(ids);
        if (_hasAnchor && !_selected.Contains(_anchor))
        {
            _anchor = default!;
            _hasAnchor = false;
        }
        if (selectFirstIfEmpty && _selected.Count == 0 && ids.Length > 0)
            SelectOnly(ids[0]);
    }

    public void SelectOnly(TKey id)
    {
        _selected.Clear();
        _selected.Add(id);
        _anchor = id;
        _hasAnchor = true;
    }

    public void Select(TKey id, IReadOnlyList<TKey> visibleIds, bool extendRange, bool toggle = false)
    {
        ArgumentNullException.ThrowIfNull(visibleIds);
        var clickedIndex = IndexOf(visibleIds, id);
        if (clickedIndex < 0)
            return;

        var anchorIndex = _hasAnchor ? IndexOf(visibleIds, _anchor) : -1;
        if (!extendRange || anchorIndex < 0)
        {
            if (toggle)
            {
                if (!_selected.Add(id))
                    _selected.Remove(id);
                _anchor = id;
                _hasAnchor = true;
            }
            else
                SelectOnly(id);
            return;
        }

        // Ctrl+Shift adds a range without clearing other selected rows.
        if (!toggle) _selected.Clear();
        for (var i = Math.Min(anchorIndex, clickedIndex);
             i <= Math.Max(anchorIndex, clickedIndex); i++)
            _selected.Add(visibleIds[i]);
    }

    private static int IndexOf(IReadOnlyList<TKey> ids, TKey id)
    {
        for (var i = 0; i < ids.Count; i++)
            if (EqualityComparer<TKey>.Default.Equals(ids[i], id))
                return i;
        return -1;
    }
}
