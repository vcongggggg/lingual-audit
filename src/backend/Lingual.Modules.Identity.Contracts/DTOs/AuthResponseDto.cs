namespace Lingual.Modules.Identity.Contracts.DTOs;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string AccessToken => Token;
    public string TokenType { get; set; } = "Bearer";
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public object User { get; set; } = null!;
    public List<string> RoleName { get; set; } = new();
    public List<string> PermissionName { get; set; } = new();
    public bool HasSystemRole { get; set; }
    public bool IsNewUser { get; set; }
}

