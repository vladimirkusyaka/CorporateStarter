using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.SecurityEvents;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security
{
    public sealed class SecurityEventRepository : ISecurityEventRepository
    {
        private readonly AppDbContext _dbContext;

        public SecurityEventRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(
            SecurityEvent securityEvent,
            CancellationToken cancellationToken)
        {
            await _dbContext.SecurityEvents.AddAsync(
                securityEvent,
                cancellationToken);
        }

        public async Task<PagedResult<SecurityEventListItemDto>> SearchAsync(
            SecurityEventSearchRequest request,
            CancellationToken cancellationToken)
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize switch
            {
                < 1 => 50,
                > 200 => 200,
                _ => request.PageSize
            };

            var query = _dbContext.SecurityEvents
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.EventType))
            {
                var eventType = request.EventType.Trim();
                query = query.Where(x => x.EventType == eventType);
            }

            if (!string.IsNullOrWhiteSpace(request.Severity))
            {
                var severity = request.Severity.Trim();
                query = query.Where(x => x.Severity == severity);
            }

            if (!string.IsNullOrWhiteSpace(request.Outcome))
            {
                var outcome = request.Outcome.Trim();
                query = query.Where(x => x.Outcome == outcome);
            }

            if (request.SubjectUserId.HasValue)
            {
                query = query.Where(x => x.SubjectUserId == request.SubjectUserId.Value);
            }

            if (request.AuthSessionId.HasValue)
            {
                query = query.Where(x => x.AuthSessionId == request.AuthSessionId.Value);
            }

            if (request.RefreshTokenFamilyId.HasValue)
            {
                query = query.Where(x => x.RefreshTokenFamilyId == request.RefreshTokenFamilyId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.CorrelationId))
            {
                var correlationId = request.CorrelationId.Trim();
                query = query.Where(x => x.CorrelationId == correlationId);
            }

            if (request.FromUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc >= request.FromUtc.Value);
            }

            if (request.ToUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc <= request.ToUtc.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SecurityEventListItemDto
                {
                    Id = x.Id,
                    CreatedAtUtc = x.CreatedAtUtc,
                    EventType = x.EventType,
                    Severity = x.Severity,
                    Outcome = x.Outcome,
                    SubjectUserId = x.SubjectUserId,
                    SubjectUserEmail = x.SubjectUserEmail,
                    AuthSessionId = x.AuthSessionId,
                    RefreshTokenFamilyId = x.RefreshTokenFamilyId,
                    CorrelationId = x.CorrelationId,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent,
                    DetailsJson = x.DetailsJson
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<SecurityEventListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
