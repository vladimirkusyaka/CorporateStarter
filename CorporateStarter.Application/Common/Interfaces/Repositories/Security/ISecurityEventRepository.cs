using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.SecurityEvents;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security
{
    public interface ISecurityEventRepository
    {
        Task AddAsync(
            SecurityEvent securityEvent,
            CancellationToken cancellationToken);

        Task<PagedResult<SecurityEventListItemDto>> SearchAsync(
            SecurityEventSearchRequest request,
            CancellationToken cancellationToken);
    }
}
