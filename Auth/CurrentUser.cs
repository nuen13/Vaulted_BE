using System.Security.Claims;

namespace Vaulted.Auth;

public static class CurrentUser
{
    // The signed-in user's id (Users.Id), from the "sub" claim of the API's own token.
    // Every endpoint except sign-in requires a token, so this is always present there.
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Signed-in user has no id."));

    // Admins (Auth:AdminEmails) may archive, unarchive and delete anyone's items and delete
    // anyone's review entries. Checked against the config on every request, so adding or
    // removing an admin takes effect without signing in again.
    public static bool IsAdmin(this ClaimsPrincipal user, IConfiguration config) =>
        IsAdminEmail(config, user.FindFirstValue("email"));

    public static bool IsAdminEmail(IConfiguration config, string? email) =>
        !string.IsNullOrWhiteSpace(email)
        && (config.GetSection("Auth:AdminEmails").Get<string[]>() ?? Array.Empty<string>())
            .Any(e => string.Equals(e.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));
}
