using System.ComponentModel.DataAnnotations;

namespace Lingual.Modules.Identity.Contracts.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
