using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace CorporateStarter.Tests.Integration.Auth
{
    internal sealed class RefreshReadBarrier : DbCommandInterceptor
    {
        private readonly TaskCompletionSource<bool> _bothRead =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _readCount;

        public int ReadCount => Volatile.Read(ref _readCount);

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (!command.CommandText.TrimStart().StartsWith(
                    "SELECT", StringComparison.OrdinalIgnoreCase) ||
                !command.CommandText.Contains(
                    "FROM \"RefreshTokens\"", StringComparison.Ordinal) ||
                !command.CommandText.Contains(
                    "\"TokenHash\" =", StringComparison.Ordinal))
            {
                return result;
            }

            var count = Interlocked.Increment(ref _readCount);

            if (count == 2)
                _bothRead.TrySetResult(true);

            // Synchronize initial reads only. Retries pass immediately.
            if (count <= 2)
            {
                await _bothRead.Task.WaitAsync(
                    TimeSpan.FromSeconds(15),
                    cancellationToken);
            }

            return result;
        }
    }
}
