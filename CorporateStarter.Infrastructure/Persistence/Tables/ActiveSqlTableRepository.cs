using CorporateStarter.Application.Common.Security;
namespace CorporateStarter.Infrastructure.Persistence.Tables;

// Query, count, find and filter-values all go through these same restrictions.
public abstract class ActiveSqlTableRepository<T>(AppDbContext db, IEntityAccess access, string resource)
    : SqlTableRepository<T>(db) where T : class
{
    protected override string SecurityPredicate => access.For(resource).ViewInactive ? "TRUE" : "\"IsActive\" = TRUE";
    protected override ISet<string> ForbiddenColumns => new HashSet<string>(
        access.For(resource).ViewInactive ? [] : ["isActive"], StringComparer.OrdinalIgnoreCase);
}
