using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Audit;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Audit
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
