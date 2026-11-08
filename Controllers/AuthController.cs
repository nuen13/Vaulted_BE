using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Vaulted.Auth;
using Vaulted.Models;

namespace Vaulted.Controllers
{
    // Sign-in: the frontend shows Google's sign-in button and sends us the ID token it gets.
    // We check the token with Google, check the email is on the allowlist (Auth:AllowedEmails),
    // then hand back our own token that the frontend sends as "Authorization: Bearer ..." on
    // every other request.
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationContext _context;
        private readonly IConfiguration _config;

        public AuthController(ApplicationContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST -> SIGN IN WITH GOOGLE
        // EXEC UpsertGoogleUser
        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<IActionResult> SignInWithGoogle([FromBody] GoogleSignInDTO dto)
        {
            var clientId = _config["Auth:GoogleClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                return StatusCode(500, "Sign-in isn't set up: Auth:GoogleClientId is missing.");

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.Credential,
                    new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized("Google sign-in failed. Try again.");
            }

            var allowed = _config.GetSection("Auth:AllowedEmails").Get<string[]>() ?? Array.Empty<string>();
            if (!payload.EmailVerified
                || !allowed.Any(e => string.Equals(e.Trim(), payload.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return StatusCode(403, $"{payload.Email} isn't on the invite list. Ask the vault owner to add you.");
            }

            try
            {
                var name = !string.IsNullOrWhiteSpace(payload.GivenName) ? payload.GivenName
                    : !string.IsNullOrWhiteSpace(payload.Name) ? payload.Name
                    : payload.Email.Split('@')[0];

                var users = await _context.Database.SqlQueryRaw<UserDTO>(
                    "EXEC UpsertGoogleUser @GoogleSub, @Email, @DisplayName, @PictureUrl",
                    new SqlParameter("@GoogleSub", payload.Subject),
                    new SqlParameter("@Email", payload.Email),
                    new SqlParameter("@DisplayName", name),
                    new SqlParameter("@PictureUrl", (object?)payload.Picture ?? DBNull.Value)
                ).ToListAsync();

                var user = users.First();
                var expiresAt = DateTime.UtcNow.AddDays(_config.GetValue("Auth:TokenLifetimeDays", 30));

                return Ok(new SignInResultDTO
                {
                    Token = CreateToken(user, expiresAt),
                    ExpiresAt = expiresAt,
                    User = user
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error signing in: {ex.Message}");
                return StatusCode(500, "An error occurred while signing in.");
            }
        }

        // GET -> WHO AM I (also tells the frontend whether its saved token still works)
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var res = await _context.Database.SqlQueryRaw<UserDTO>(
                "EXEC GetUserById @UserId", new SqlParameter("@UserId", User.GetUserId())
            ).ToListAsync();

            var user = res.FirstOrDefault();
            return user == null ? Unauthorized() : Ok(user);
        }

        // GET -> EVERYONE IN THE VAULT (to show who added an item)
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var res = await _context.Database.SqlQueryRaw<UserDTO>("EXEC GetAllUsers").ToListAsync();
            return Ok(res);
        }

        private string CreateToken(UserDTO user, DateTime expiresAt)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Auth:JwtKey"]!));
            return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = AuthSetup.Issuer,
                Audience = AuthSetup.Issuer,
                Expires = expiresAt,
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", user.Id.ToString()),
                    new Claim("name", user.DisplayName),
                    new Claim("email", user.Email)
                }),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            });
        }
    }
}
