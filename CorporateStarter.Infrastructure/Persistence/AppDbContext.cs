using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using CorporateStarter.Application.Common.Interfaces;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Core.Entities.Audit;
using CorporateStarter.Core.Entities.Directory;
using CorporateStarter.Core.Entities.MasterData;
using CorporateStarter.Core.Entities.Security;

namespace CorporateStarter.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<RefreshTokenFamily> RefreshTokenFamilies => Set<RefreshTokenFamily>();
    public DbSet<LoginAttemptState> LoginAttemptStates => Set<LoginAttemptState>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<Person> People => Set<Person>();

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public override Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
    {
        AddAuditLogs();

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureRoles(modelBuilder);
        ConfigureUserRoles(modelBuilder);
        ConfigurePermissions(modelBuilder);
        ConfigureRolePermissions(modelBuilder);
        ConfigureAuthSessions(modelBuilder);
        ConfigureRefreshTokenFamilies(modelBuilder);
        ConfigureRefreshTokens(modelBuilder);
        ConfigureSecurityEvents(modelBuilder);
        ConfigureLoginAttemptStates(modelBuilder);
        ConfigurePasswordHistories(modelBuilder);
        ConfigurePeople(modelBuilder);

        ConfigureCompanies(modelBuilder);
        ConfigurePositions(modelBuilder);
        ConfigureCountries(modelBuilder);
        ConfigureCities(modelBuilder);

        ConfigureAuditLogs(modelBuilder);
    }

    private void AddAuditLogs()
    {
        ChangeTracker.DetectChanges();

        var auditLogs = ChangeTracker
            .Entries()
            .Where(x =>
                x.Entity is not AuditLog &&
                x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(CreateAuditLog)
            .Where(x => x is not null)
            .Cast<AuditLog>()
            .ToArray();

        if (auditLogs.Length > 0)
            AuditLogs.AddRange(auditLogs);
    }

    private AuditLog? CreateAuditLog(EntityEntry entry)
    {
        var entityName = entry.Metadata.ClrType.Name;
        var entityId = GetEntityId(entry);

        if (string.IsNullOrWhiteSpace(entityId))
            return null;

        var action = GetAuditAction(entry);
        var oldValues = entry.State == EntityState.Added
            ? null
            : GetPropertyValues(entry, useOriginalValues: true);

        var newValues = entry.State == EntityState.Deleted
            ? null
            : GetPropertyValues(entry, useOriginalValues: false);

        return new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            UserEmail = GetAuditUser(),
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();

        if (primaryKey is null)
            return string.Empty;

        var values = primaryKey.Properties
            .Select(property =>
            {
                var value = entry.Property(property.Name).CurrentValue;
                return $"{property.Name}:{value}";
            });

        return string.Join("|", values);
    }

    private static string GetAuditAction(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
            return "Create";

        if (entry.State == EntityState.Deleted)
            return "Delete";

        var isActiveProperty = entry.Properties
            .FirstOrDefault(x => x.Metadata.Name == "IsActive");

        if (isActiveProperty is not null &&
            isActiveProperty.IsModified &&
            isActiveProperty.OriginalValue is true &&
            isActiveProperty.CurrentValue is false)
        {
            return "Deactivate";
        }

        return "Update";
    }

    private static Dictionary<string, object?> GetPropertyValues(
    EntityEntry entry,
    bool useOriginalValues)
    {
        return entry.Properties
            .Where(x => !x.Metadata.IsPrimaryKey())
            .Where(x => x.Metadata.Name != "PasswordHash")
            .Where(x => x.Metadata.Name != "TokenHash")
            .Where(x => x.Metadata.Name != "ReplacedByTokenHash")
            .Where(x => !x.Metadata.IsShadowProperty())
            .ToDictionary(
                x => x.Metadata.Name,
                x => useOriginalValues ? x.OriginalValue : x.CurrentValue);
    }

    private string GetAuditUser()
    {
        if (!string.IsNullOrWhiteSpace(_currentUserService?.Email))
            return _currentUserService.Email;

        if (!string.IsNullOrWhiteSpace(_currentUserService?.Login))
            return _currentUserService.Login;

        return "system";
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Login)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(x => x.Login)
                .IsUnique();

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.Property(x => x.PasswordHash)
                .IsRequired();

            entity.Property(x => x.DisplayName)
                .HasMaxLength(256);

            entity.HasOne(x => x.Person)
                .WithOne(x => x.User)
                .HasForeignKey<User>(x => x.PersonId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(x => x.AuthSessions)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.RefreshTokenFamilies)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.Property(x => x.Description)
                .HasMaxLength(500);
        });
    }

    private static void ConfigureUserRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");

            entity.HasKey(x => new { x.UserId, x.RoleId });

            entity.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePermissions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.Group)
                .IsRequired()
                .HasMaxLength(100);
        });
    }

    private static void ConfigureRolePermissions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");

            entity.HasKey(x => new { x.RoleId, x.PermissionId });

            entity.HasOne(x => x.Role)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePeople(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("People");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(100);

            entity.HasIndex(x => x.Code);

            entity.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.MiddleName)
                .HasMaxLength(100);

            entity.Property(x => x.LastName)
                .HasMaxLength(100);

            entity.Property(x => x.DateOfBirth)
                .HasColumnType("date");

            entity.Property(x => x.Email)
                .HasMaxLength(256);

            entity.Property(x => x.Phone)
                .HasMaxLength(50);

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.HasOne(x => x.Company)
                .WithMany(x => x.People)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Position)
                .WithMany(x => x.People)
                .HasForeignKey(x => x.PositionId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureCompanies(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Companies");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(100);

            entity.HasIndex(x => x.Code);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(x => x.LegalName)
                .HasMaxLength(256);

            entity.Property(x => x.TaxNumber)
                .HasMaxLength(100);

            entity.Property(x => x.VatId)
                .HasMaxLength(100);

            entity.Property(x => x.Email)
                .HasMaxLength(256);

            entity.Property(x => x.Phone)
                .HasMaxLength(50);

            entity.Property(x => x.Website)
                .HasMaxLength(256);

            entity.Property(x => x.Street)
                .HasMaxLength(256);

            entity.Property(x => x.HouseNumber)
                .HasMaxLength(50);

            entity.Property(x => x.PostalCode)
                .HasMaxLength(20);

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.HasOne(x => x.City)
                .WithMany(x => x.Companies)
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigurePositions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Position>(entity =>
        {
            entity.ToTable("Positions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.Property(x => x.Description)
                .HasMaxLength(1000);
        });
    }

    private static void ConfigureCountries(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("Countries");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(2);

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.NativeName)
                .HasMaxLength(150);

            entity.Property(x => x.PhoneCode)
                .HasMaxLength(10);
        });
    }

    private static void ConfigureCities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>(entity =>
        {
            entity.ToTable("Cities");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Region)
                .HasMaxLength(150);

            entity.HasIndex(x => new { x.CountryId, x.Name, x.Region });

            entity.HasOne(x => x.Country)
                .WithMany(x => x.Cities)
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.EntityName)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(x => x.EntityId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.UserEmail)
                .HasMaxLength(256);
        });
    }

    private static void ConfigureRefreshTokens(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.TokenHash)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(x => x.ReplacedByTokenHash)
                .HasMaxLength(128);

            entity.Property(x => x.CreatedByIp)
                .HasMaxLength(64);

            entity.Property(x => x.RevokedByIp)
                .HasMaxLength(64);

            entity.Property(x => x.UserAgent)
                .HasMaxLength(512);

            entity.Property(x => x.JwtId)
                .HasMaxLength(64);

            entity.HasIndex(x => x.TokenHash)
                .IsUnique();

            entity.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });

            entity.HasIndex(x => x.AuthSessionId);

            entity.HasIndex(x => x.RefreshTokenFamilyId);

            entity.HasIndex(x => x.JwtId);

            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.AuthSession)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.AuthSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.RefreshTokenFamily)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.RefreshTokenFamilyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuthSessions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.ToTable("AuthSessions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.CreatedByIp)
                .HasMaxLength(64);

            entity.Property(x => x.RevokedByIp)
                .HasMaxLength(64);

            entity.Property(x => x.UserAgent)
                .HasMaxLength(512);

            entity.Property(x => x.DeviceName)
                .HasMaxLength(256);

            entity.Property(x => x.CurrentJwtId)
                .HasMaxLength(64);

            entity.HasIndex(x => new { x.UserId, x.RevokedAtUtc });

            entity.HasIndex(x => x.CurrentJwtId);

            entity.HasMany(x => x.RefreshTokenFamilies)
                .WithOne(x => x.AuthSession)
                .HasForeignKey(x => x.AuthSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.RefreshTokens)
                .WithOne(x => x.AuthSession)
                .HasForeignKey(x => x.AuthSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRefreshTokenFamilies(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshTokenFamily>(entity =>
        {
            entity.ToTable("RefreshTokenFamilies");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.RevokedByIp)
                .HasMaxLength(64);

            entity.Property(x => x.ReuseDetectedByIp)
                .HasMaxLength(64);

            entity.HasIndex(x => new { x.UserId, x.RevokedAtUtc });

            entity.HasIndex(x => new { x.AuthSessionId, x.RevokedAtUtc });

            entity.HasMany(x => x.RefreshTokens)
                .WithOne(x => x.RefreshTokenFamily)
                .HasForeignKey(x => x.RefreshTokenFamilyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSecurityEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SecurityEvent>(entity =>
        {
            entity.ToTable("SecurityEvents");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.EventType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Severity)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(x => x.Outcome)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(x => x.SubjectUserEmail)
                .HasMaxLength(256);

            entity.Property(x => x.CorrelationId)
                .HasMaxLength(100);

            entity.Property(x => x.IpAddress)
                .HasMaxLength(64);

            entity.Property(x => x.UserAgent)
                .HasMaxLength(512);

            entity.HasIndex(x => x.CreatedAtUtc);

            entity.HasIndex(x => new { x.EventType, x.CreatedAtUtc });

            entity.HasIndex(x => new { x.SubjectUserId, x.CreatedAtUtc });

            entity.HasIndex(x => new { x.AuthSessionId, x.CreatedAtUtc });

            entity.HasIndex(x => new { x.RefreshTokenFamilyId, x.CreatedAtUtc });

            entity.HasIndex(x => x.CorrelationId);
        });
    }

    private static void ConfigureLoginAttemptStates(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoginAttemptState>(entity =>
        {
            entity.ToTable("LoginAttemptStates");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.LoginIdentifierHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.LastIpAddress)
                .HasMaxLength(64);

            entity.Property(x => x.LastUserAgent)
                .HasMaxLength(512);

            entity.HasIndex(x => x.LoginIdentifierHash)
                .IsUnique();

            entity.HasIndex(x => x.UserId);

            entity.HasIndex(x => x.LockedUntilUtc);
        });
    }

    private static void ConfigurePasswordHistories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PasswordHistory>(entity =>
        {
            entity.ToTable("PasswordHistories");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.PasswordHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
