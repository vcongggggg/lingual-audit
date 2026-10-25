namespace Lingual.Modules.Identity.Infrastructure.Auth;

public class MezonOAuth2Options
{
    public const string SectionName = "MezonOAuth2";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AuthorizeUrl { get; set; } = "https://oauth2.mezon.ai/oauth2/auth";
    public string TokenUrl { get; set; } = "https://oauth2.mezon.ai/oauth2/token";
    public string UserInfoUrl { get; set; } = "https://oauth2.mezon.ai/userinfo";
    public string RedirectUri { get; set; } = string.Empty;
    public string Scope { get; set; } = "openid offline";
}
