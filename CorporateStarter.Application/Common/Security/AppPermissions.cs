using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Application.Common.Security
{
    public static class AppPermissions
    {
        public const string PreferencesUpdate = "preferences.update";
        public const string PreferencesConnectionsRead = "preferences.connections.read";
        public const string AuditRead = "audit.read";

        public const string CompaniesViewInactive = "companies.view_inactive";
        public const string CompaniesRestore = "companies.restore";
        public const string CompaniesRead = "companies.read";
        public const string CompaniesCreate = "companies.create";
        public const string CompaniesUpdate = "companies.update";
        public const string CompaniesDelete = "companies.delete";

        public const string PersonsViewInactive = "persons.view_inactive";
        public const string PersonsRestore = "persons.restore";
        public const string PersonsRead = "persons.read";
        public const string PersonsCreate = "persons.create";
        public const string PersonsUpdate = "persons.update";
        public const string PersonsDelete = "persons.delete";

        public const string CountriesRead = "countries.read";
        public const string CountriesCreate = "countries.create";
        public const string CountriesUpdate = "countries.update";
        public const string CountriesViewInactive = "countries.view_inactive";
        public const string CountriesRestore = "countries.restore";
        public const string CountriesDelete = "countries.delete";

        public const string CitiesViewInactive = "cities.view_inactive";
        public const string CitiesRestore = "cities.restore";
        public const string CitiesRead = "cities.read";
        public const string CitiesCreate = "cities.create";
        public const string CitiesUpdate = "cities.update";
        public const string CitiesDelete = "cities.delete";

        public const string PositionsViewInactive = "positions.view_inactive";
        public const string PositionsRestore = "positions.restore";
        public const string PositionsRead = "positions.read";
        public const string PositionsCreate = "positions.create";
        public const string PositionsUpdate = "positions.update";
        public const string PositionsDelete = "positions.delete";

        public const string UsersViewInactive = "users.view_inactive";
        public const string UsersRestore = "users.restore";
        public const string UsersRead = "users.read";
        public const string UsersCreate = "users.create";
        public const string UsersUpdate = "users.update";
        public const string UsersDelete = "users.delete";
        public const string UsersChangePassword = "users.change_password";

        public const string RolesViewInactive = "roles.view_inactive";
        public const string RolesRestore = "roles.restore";
        public const string RolesRead = "roles.read";
        public const string RolesManagePermissions = "roles.manage_permissions";
        public const string RolesCreate = "roles.create";
        public const string RolesUpdate = "roles.update";
        public const string RolesDelete = "roles.delete";

        public const string PermissionsViewInactive = "permissions.view_inactive";
        public const string PermissionsRead = "permissions.read";

        public static IReadOnlyList<PermissionDefinition> All { get; } =
        [
            new(PreferencesUpdate, "Update application preferences", "Preferences"),
            new(PreferencesConnectionsRead, "View application connections", "Preferences"),
            new(AuditRead, "Read audit log", "Security"),

        new(CompaniesViewInactive, "View inactive companies", "Companies"),
        new(CompaniesRestore, "Restore companies", "Companies"),
        new(CompaniesRead, "Read companies", "Companies"),
        new(CompaniesCreate, "Create companies", "Companies"),
        new(CompaniesUpdate, "Update companies", "Companies"),
        new(CompaniesDelete, "Delete companies", "Companies"),

        new(PersonsViewInactive, "View inactive persons", "Persons"),
        new(PersonsRestore, "Restore persons", "Persons"),
        new(PersonsRead, "Read persons", "Persons"),
        new(PersonsCreate, "Create persons", "Persons"),
        new(PersonsUpdate, "Update persons", "Persons"),
        new(PersonsDelete, "Delete persons", "Persons"),

        new(CountriesRead, "Read countries", "Countries"),
        new(CountriesCreate, "Create countries", "Countries"),
        new(CountriesUpdate, "Update countries", "Countries"),
        new(CountriesViewInactive, "View inactive countries", "Countries"),
        new(CountriesRestore, "Restore countries", "Countries"),
        new(CountriesDelete, "Delete countries", "Countries"),

        new(CitiesViewInactive, "View inactive cities", "Cities"),
        new(CitiesRestore, "Restore cities", "Cities"),
        new(CitiesRead, "Read cities", "Cities"),
        new(CitiesCreate, "Create cities", "Cities"),
        new(CitiesUpdate, "Update cities", "Cities"),
        new(CitiesDelete, "Delete cities", "Cities"),

        new(PositionsViewInactive, "View inactive positions", "Positions"),
        new(PositionsRestore, "Restore positions", "Positions"),
        new(PositionsRead, "Read positions", "Positions"),
        new(PositionsCreate, "Create positions", "Positions"),
        new(PositionsUpdate, "Update positions", "Positions"),
        new(PositionsDelete, "Delete positions", "Positions"),

        new(UsersViewInactive, "View inactive users", "Security"),
        new(UsersRestore, "Restore users", "Security"),
        new(UsersRead, "Read users", "Security"),
        new(UsersCreate, "Create users", "Security"),
        new(UsersUpdate, "Update users", "Security"),
        new(UsersDelete, "Delete users", "Security"),
        new(UsersChangePassword, "Change user password", "Security"),

        new(RolesViewInactive, "View inactive roles", "Security"),
        new(RolesRestore, "Restore roles", "Security"),
        new(RolesRead, "Read roles", "Security"),
        new(RolesManagePermissions, "Manage role permissions", "Security"),
        new(RolesCreate, "Create roles", "Security"),
        new(RolesUpdate, "Update roles", "Security"),
        new(RolesDelete, "Delete roles", "Security"),

        new(PermissionsViewInactive, "View inactive permissions", "Security"),
        new(PermissionsRead, "Read permissions", "Security")
        ];
    }
}
