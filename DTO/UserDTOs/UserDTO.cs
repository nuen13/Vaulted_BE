public class UserDTO
{
    public Guid Id { get; set; }

    public string Email { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string? PictureUrl { get; set; }
}

// POST api/Auth/google: the ID token ("credential") from Google's sign-in button
public class GoogleSignInDTO
{
    public string Credential { get; set; } = "";
}

public class SignInResultDTO
{
    public string Token { get; set; } = "";

    public DateTime ExpiresAt { get; set; }

    public UserDTO User { get; set; } = new();
}
