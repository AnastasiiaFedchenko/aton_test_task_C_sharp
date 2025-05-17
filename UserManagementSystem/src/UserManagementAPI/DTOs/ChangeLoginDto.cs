using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.DTOs;

public class ChangeLoginDto
{
    [Required, RegularExpression(@"^[a-zA-Z0-9]+$")]
    public string NewLogin { get; set; } = string.Empty;
}
