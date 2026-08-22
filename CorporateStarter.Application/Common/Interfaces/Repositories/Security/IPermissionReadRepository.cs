using System;
using System.Text;
using System.Collections.Generic;
using CorporateStarter.Shared.Dtos.Permissions;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Security
{
    public interface IPermissionReadRepository
    {
        Task<IReadOnlyList<PermissionListItemDto>> GetListAsync(
            CancellationToken cancellationToken);
    }
}
