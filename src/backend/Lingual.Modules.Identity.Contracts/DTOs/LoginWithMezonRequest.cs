namespace Lingual.Modules.Identity.Contracts.DTOs;

public class LoginWithMezonRequest
{
    public string? Code { get; set; }

    public string? State { get; set; }

    public string? Token { get; set; }

    public string? MezonUserId { get; set; }

    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? RedirectUri { get; set; }

    public string EffectiveToken =>
        !string.IsNullOrWhiteSpace(Code) ? Code :
        !string.IsNullOrWhiteSpace(Token) ? Token : string.Empty;
}
