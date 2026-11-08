using System.Security.Claims;

namespace Vaulted.Auth;

public static class CurrentUser
{
    // The signed-in user's id (Users.Id), from the "sub" claim of the API's own token.
    // Every endpoint except sign-in requires a token, so this is always present there.
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Signed-in user has no id."));
}
