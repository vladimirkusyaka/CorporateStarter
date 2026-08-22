using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Interfaces.Security
{
    public interface ICorrelationIdProvider
    {
        string? CorrelationId { get; }
    }
}
