using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class PasswordHistoryOptions
    {
        public const string SectionName = "PasswordHistory";

        public int HistoryCount { get; set; } = 5;
    }
}
