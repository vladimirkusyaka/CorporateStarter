using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Interfaces.Security
{
    public interface IAuthTransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken);

        Task RollbackAsync(CancellationToken cancellationToken);
    }
}
