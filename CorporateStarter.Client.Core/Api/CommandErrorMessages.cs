namespace CorporateStarter.Client.Core.Api;

public static class CommandErrorMessages
{
    public static string? ForCode(string? code) => code switch
    {
        "user.login_already_exists" => "A user with this login already exists.",
        "user.email_already_exists" => "A user with this email already exists.",
        "user.role_required" => "Select at least one active role for an active user.",
        "user.roles_not_found" => "One or more selected roles are unavailable. Reload the user and select active roles.",
        "user.self_deactivation_not_allowed" => "You cannot deactivate your own user account.",
        "user.last_administrator_cannot_be_deactivated" => "The last active administrator must remain active and retain the Administrator role.",
        "user.password_policy_failed" => "The password does not meet the server password policy. Choose a stronger password that does not contain the login or email.",
        "user.password_reused" => "This password was used recently. Choose a different password.",
        "user.password_required" => "Enter a password.",
        "user.login_invalid" or "user.login_required" => "Enter a login of 3–100 characters.",
        "user.email_invalid" or "user.email_required" => "Enter a valid email address.",
        "company.name_required" => "Enter a company name.",
        "company.name_exists" => "A company with this name already exists.",
        "company.code_exists" => "A company with this code already exists.",
        "company.city_not_found" => "The selected city no longer exists. Choose another city or clear the selection.",
        _ => null
    };
}
