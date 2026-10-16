using CorporateStarter.Application.Common.Security;
using Microsoft.EntityFrameworkCore;
namespace CorporateStarter.Infrastructure.Persistence;

public static class VisibleRecords
{
    // Explicit opt-in: authentication, seeding and uniqueness checks remain unfiltered.
    public static IQueryable<T> VisibleTo<T>(this IQueryable<T> query, IEntityAccess access, string resource)
        where T : class => access.For(resource).ViewInactive
            ? query : query.Where(x => EF.Property<bool>(x, "IsActive"));
}
