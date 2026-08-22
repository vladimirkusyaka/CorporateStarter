using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Application.Common.Security
{
    public static class AppPermissions
    {
        public const string AuditRead = "audit.read";

        public const string CompaniesRead = "companies.read";
        public const string CompaniesCreate = "companies.create";
        public const string CompaniesUpdate = "companies.update";
        public const string CompaniesDelete = "companies.delete";

        public const string PersonsRead = "persons.read";
        public const string PersonsCreate = "persons.create";
        public const string PersonsUpdate = "persons.update";
        public const string PersonsDelete = "persons.delete";

        public const string CountriesRead = "countries.read";
        public const string CountriesCreate = "countries.create";
        public const string CountriesUpdate = "countries.update";
        public const string CountriesDelete = "countries.delete";

        public const string CitiesRead = "cities.read";
        public const string CitiesCreate = "cities.create";
        public const string CitiesUpdate = "cities.update";
        public const string CitiesDelete = "cities.delete";

        public const string PositionsRead = "positions.read";
        public const string PositionsCreate = "positions.create";
        public const string PositionsUpdate = "positions.update";
        public const string PositionsDelete = "positions.delete";

        public const string UsersRead = "users.read";
        public const string UsersCreate = "users.create";
        public const string UsersUpdate = "users.update";
        public const string UsersDelete = "users.delete";
        public const string UsersChangePassword = "users.change_password";

        public const string RolesRead = "roles.read";
        public const string RolesManagePermissions = "roles.manage_permissions";
        public const string RolesCreate = "roles.create";
        public const string RolesUpdate = "roles.update";
        public const string RolesDelete = "roles.delete";

        public const string PermissionsRead = "permissions.read";

        public static IReadOnlyList<PermissionDefinition> All { get; } =
        [
            new(AuditRead, "Read audit log", "Security"),

        new(CompaniesRead, "Read companies", "Companies"),
        new(CompaniesCreate, "Create companies", "Companies"),
        new(CompaniesUpdate, "Update companies", "Companies"),
        new(CompaniesDelete, "Delete companies", "Companies"),

        new(PersonsRead, "Read persons", "Persons"),
        new(PersonsCreate, "Create persons", "Persons"),
        new(PersonsUpdate, "Update persons", "Persons"),
        new(PersonsDelete, "Delete persons", "Persons"),

        new(CountriesRead, "Read countries", "Countries"),
        new(CountriesCreate, "Create countries", "Countries"),
        new(CountriesUpdate, "Update countries", "Countries"),
        new(CountriesDelete, "Delete countries", "Countries"),

        new(CitiesRead, "Read cities", "Cities"),
        new(CitiesCreate, "Create cities", "Cities"),
        new(CitiesUpdate, "Update cities", "Cities"),
        new(CitiesDelete, "Delete cities", "Cities"),

        new(PositionsRead, "Read positions", "Positions"),
        new(PositionsCreate, "Create positions", "Positions"),
        new(PositionsUpdate, "Update positions", "Positions"),
        new(PositionsDelete, "Delete positions", "Positions"),

        new(UsersRead, "Read users", "Security"),
        new(UsersCreate, "Create users", "Security"),
        new(UsersUpdate, "Update users", "Security"),
        new(UsersDelete, "Delete users", "Security"),
        new(UsersChangePassword, "Change user password", "Security"),

        new(RolesRead, "Read roles", "Security"),
        new(RolesManagePermissions, "Manage role permissions", "Security"),
        new(RolesCreate, "Create roles", "Security"),
        new(RolesUpdate, "Update roles", "Security"),
        new(RolesDelete, "Delete roles", "Security"),

        new(PermissionsRead, "Read permissions", "Security")
        ];
    }
}
