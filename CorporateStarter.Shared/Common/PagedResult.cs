using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Shared.Common
{
    public sealed class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = [];

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }
    }
}
