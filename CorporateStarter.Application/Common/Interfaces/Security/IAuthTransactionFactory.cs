using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Interfaces.Security
{
    public interface IAuthTransactionFactory
    {
        Task<IAuthTransaction> BeginSerializableAsync(
            CancellationToken cancellationToken);
    }
}
