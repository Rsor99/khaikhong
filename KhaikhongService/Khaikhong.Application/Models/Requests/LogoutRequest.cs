using System.ComponentModel.DataAnnotations;

namespace Khaikhong.Application.Models.Requests;

public sealed class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

