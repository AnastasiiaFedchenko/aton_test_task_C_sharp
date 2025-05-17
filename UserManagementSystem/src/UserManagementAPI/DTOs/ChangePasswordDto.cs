using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.DTOs;

public class ChangePasswordDto
{
    [Required, RegularExpression(@"^[a-zA-Z0-9]+$")]
    public string NewPassword { get; set; } = string.Empty;
}
