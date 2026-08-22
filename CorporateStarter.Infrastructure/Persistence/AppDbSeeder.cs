using Microsoft.EntityFrameworkCore;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Core.Entities.Directory;
using CorporateStarter.Core.Entities.MasterData;
using CorporateStarter.Core.Entities.Security;

namespace CorporateStarter.Infrastructure.Persistence;

public static class AppDbSeeder
{
    public static async Task SeedAsync(
    AppDbContext context,
    InitialAdminOptions initialAdminOptions)
    {
 /**/       await SeedRolesAsync(context);
        await SeedPermissionsAsync(context);

        await context.SaveChangesAsync();

        await SeedRolePermissionsAsync(context);

        await SeedCountriesAndCitiesAsync(context);
        await SeedPositionsAsync(context);

        await context.SaveChangesAsync();

        await SeedAdministratorAsync(context, initialAdminOptions);

        await context.SaveChangesAsync();
    }

    private static async Task SeedPermissionsAsync(AppDbContext context)
    {
        var definitions = AppPermissions.All;
        var definitionCodes = definitions
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingPermissions = await context.Permissions.ToListAsync();

        foreach (var definition in definitions)
        {
            var permission = existingPermissions
                .FirstOrDefault(x => x.Code == definition.Code);

            if (permission is null)
            {
                await context.Permissions.AddAsync(new Permission
                {
                    Code = definition.Code,
                    Name = definition.Name,
                    Group = definition.Group,
                    Description = definition.Description,
                    IsActive = true
                });

                continue;
            }

            permission.Name = definition.Name;
            permission.Group = definition.Group;
            permission.Description = definition.Description;
            permission.IsActive = true;
            permission.UpdatedAtUtc = DateTime.UtcNow;
        }

        foreach (var permission in existingPermissions)
        {
            if (!definitionCodes.Contains(permission.Code))
            {
                permission.IsActive = false;
                permission.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
    }

    private static async Task SeedRolePermissionsAsync(AppDbContext context)
    {
        var roles = await context.Roles.ToListAsync();
        var permissions = await context.Permissions
            .Where(x => x.IsActive)
            .ToListAsync();

        var administratorRole = roles.FirstOrDefault(x => x.Name == "Administrator");
        var managerRole = roles.FirstOrDefault(x => x.Name == "Manager");
        var employeeRole = roles.FirstOrDefault(x => x.Name == "Mitarbeiter");

        if (administratorRole is not null)
        {
            await AddMissingRolePermissionsAsync(
                context,
                administratorRole,
                permissions.Select(x => x.Code));
        }

        if (managerRole is not null)
        {
            await AddMissingRolePermissionsAsync(
                context,
                managerRole,
                [
                    AppPermissions.CompaniesRead,
                AppPermissions.CompaniesCreate,
                AppPermissions.CompaniesUpdate,

                AppPermissions.PersonsRead,
                AppPermissions.PersonsCreate,
                AppPermissions.PersonsUpdate,

                AppPermissions.CountriesRead,
                AppPermissions.CitiesRead,
                AppPermissions.PositionsRead
                ]);
        }

        if (employeeRole is not null)
        {
            await AddMissingRolePermissionsAsync(
                context,
                employeeRole,
                [
                    AppPermissions.CompaniesRead,
                AppPermissions.PersonsRead,
                AppPermissions.CountriesRead,
                AppPermissions.CitiesRead,
                AppPermissions.PositionsRead
                ]);
        }
    }

    private static async Task AddMissingRolePermissionsAsync(
    AppDbContext context,
    Role role,
    IEnumerable<string> permissionCodes)
    {
        var codes = permissionCodes
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var permissions = await context.Permissions
            .Where(x => x.IsActive && codes.Contains(x.Code))
            .ToListAsync();

        var existingPermissionIds = await context.RolePermissions
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.PermissionId)
            .ToListAsync();

        foreach (var permission in permissions)
        {
            if (existingPermissionIds.Contains(permission.Id))
                continue;

            await context.RolePermissions.AddAsync(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            });
        }
    }

    private static async Task SeedRolesAsync(AppDbContext context)
    {
        if (await context.Roles.AnyAsync())
            return;

        var roles = new[]
        {
            new Role
            {
                Name = "Administrator",
                Description = "Vollzugriff auf das System",
                IsSystemRole = true,
                IsActive = true
            },
            new Role
            {
                Name = "Manager",
                Description = "Verwaltung von Daten und Prozessen",
                IsSystemRole = true,
                IsActive = true
            },
            new Role
            {
                Name = "Mitarbeiter",
                Description = "Standardbenutzer mit eingeschränkten Rechten",
                IsSystemRole = true,
                IsActive = true
            }
        };

        await context.Roles.AddRangeAsync(roles);
    }

    private static async Task SeedCountriesAndCitiesAsync(AppDbContext context)
    {
        if (await context.Countries.AnyAsync())
            return;

        var germany = new Country
        {
            Code = "DE",
            Name = "Germany",
            NativeName = "Deutschland",
            PhoneCode = "+49",
            IsActive = true
        };

        var austria = new Country
        {
            Code = "AT",
            Name = "Austria",
            NativeName = "Österreich",
            PhoneCode = "+43",
            IsActive = true
        };

        var switzerland = new Country
        {
            Code = "CH",
            Name = "Switzerland",
            NativeName = "Schweiz",
            PhoneCode = "+41",
            IsActive = true
        };

        await context.Countries.AddRangeAsync(germany, austria, switzerland);

        var cities = new[]
        {
            new City { Name = "Friedrichshafen", Region = "Baden-Württemberg", Country = germany, IsActive = true },
            new City { Name = "Lindau", Region = "Bayern", Country = germany, IsActive = true },
            new City { Name = "München", Region = "Bayern", Country = germany, IsActive = true },
            new City { Name = "Stuttgart", Region = "Baden-Württemberg", Country = germany, IsActive = true },
            new City { Name = "Berlin", Region = "Berlin", Country = germany, IsActive = true },
            new City { Name = "Hamburg", Region = "Hamburg", Country = germany, IsActive = true },

            new City { Name = "Wien", Region = "Wien", Country = austria, IsActive = true },

            new City { Name = "Zürich", Region = "Zürich", Country = switzerland, IsActive = true }
        };

        await context.Cities.AddRangeAsync(cities);
    }

    private static async Task SeedPositionsAsync(AppDbContext context)
    {
        if (await context.Positions.AnyAsync())
            return;

        var positions = new[]
        {
            new Position { Name = "Geschäftsführer", Description = "Unternehmensleitung", IsActive = true },
            new Position { Name = "Abteilungsleiter", Description = "Leitung einer Abteilung", IsActive = true },
            new Position { Name = "Projektleiter", Description = "Verantwortlich für Projekte", IsActive = true },
            new Position { Name = "Entwickler", Description = "Softwareentwicklung", IsActive = true },
            new Position { Name = "Buchhalter", Description = "Buchhaltung und Finanzen", IsActive = true },
            new Position { Name = "Vertriebsmitarbeiter", Description = "Vertrieb und Kundenbetreuung", IsActive = true },
            new Position { Name = "Sachbearbeiter", Description = "Administrative Tätigkeiten", IsActive = true }
        };

        await context.Positions.AddRangeAsync(positions);
    }

    private static async Task SeedAdministratorAsync(
            AppDbContext context,
            InitialAdminOptions initialAdminOptions)
    {
        if (string.IsNullOrWhiteSpace(initialAdminOptions.Password))
            return;

        var login = initialAdminOptions.Login.Trim();
        var email = initialAdminOptions.Email.Trim();

        if (await context.Users.AnyAsync(x => x.Login == login || x.Email == email))
            return;

        var administratorRole = await context.Roles
            .SingleAsync(x => x.Name == "Administrator");

        var adminPerson = new Person
        {
            Code = "SYS-ADMIN",
            FirstName = "System",
            LastName = "Administrator",
            Email = email,
            Description = "Initialer Systemadministrator",
            IsActive = true
        };

        var adminUser = new User
        {
            Login = login,
            Email = email,
            DisplayName = initialAdminOptions.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(initialAdminOptions.Password),
            Person = adminPerson,
            IsActive = true
        };

        var adminUserRole = new UserRole
        {
            User = adminUser,
            Role = administratorRole
        };

        await context.Users.AddAsync(adminUser);
        await context.UserRoles.AddAsync(adminUserRole);
    }
}