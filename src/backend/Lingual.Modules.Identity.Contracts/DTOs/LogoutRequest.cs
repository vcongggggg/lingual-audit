using System.ComponentModel.DataAnnotations;

namespace Lingual.Modules.Identity.Contracts.DTOs;

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}