using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Vaulted.Auth;

public static class AuthSetup
{
    public const string Issuer = "vaulted";

    // Every endpoint needs a signed-in user unless it says [AllowAnonymous].
    public static void AddVaultedAuth(this IServiceCollection services, IConfiguration config)
    {
        var jwtKey = config["Auth:JwtKey"];
        if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
            throw new InvalidOperationException(
                "Auth:JwtKey must be set to a random secret of at least 32 characters " +
                "(appsettings.Development.json locally, an environment variable Auth__JwtKey when hosted).");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep "sub"/"name" as-is
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = Issuer,
                    ValidAudience = Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    NameClaimType = "name",
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        });
    }
}
