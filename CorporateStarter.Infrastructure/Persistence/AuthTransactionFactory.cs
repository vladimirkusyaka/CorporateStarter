using System.Data;
using CorporateStarter.Application.Common.Interfaces.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CorporateStarter.Infrastructure.Persistence;

public sealed class AuthTransactionFactory : IAuthTransactionFactory
{
    private readonly AppDbContext _dbContext;

    public AuthTransactionFactory(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IAuthTransaction> BeginSerializableAsync(
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.CurrentTransaction is not null ||
            System.Transactions.Transaction.Current is not null)
        {
            throw new InvalidOperationException(
                "Nested auth transactions are not supported.");
        }

        var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        return new AuthTransaction(transaction);
    }

    private sealed class AuthTransaction : IAuthTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public AuthTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync(CancellationToken cancellationToken)
            => _transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken)
            => _transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync()
            => _transaction.DisposeAsync();
    }
}
