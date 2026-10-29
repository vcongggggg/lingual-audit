namespace Lingual.Modules.Identity.Contracts.DTOs;

public class MezonAuthRequest
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? RedirectUri { get; set; }
}

