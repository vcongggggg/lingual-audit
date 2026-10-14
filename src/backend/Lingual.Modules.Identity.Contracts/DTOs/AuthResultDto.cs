namespace Lingual.Modules.Identity.Contracts.DTOs;

public class AuthResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public UserDto User { get; set; } = null!;
    public bool IsNewUser { get; set; }
    public bool OnboardingCompleted { get; set; }
}
