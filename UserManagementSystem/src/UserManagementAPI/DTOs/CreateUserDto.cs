using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.DTOs;

public class CreateUserDto
{
    [Required, RegularExpression(@"^[a-zA-Z0-9]+$")]
    public string Login { get; set; } = string.Empty;
    
    [Required, RegularExpression(@"^[a-zA-Z0-9]+$")]
    public string Password { get; set; } = string.Empty;
    
    [Required, RegularExpression(@"^[a-zA-Zà-ÿÀ-ß]+$")]
    public string Name { get; set; } = string.Empty;
    
    [Range(0, 2)]
    public int Gender { get; set; }
    
    public DateTime? Birthday { get; set; }
    
    public bool Admin { get; set; }
}
