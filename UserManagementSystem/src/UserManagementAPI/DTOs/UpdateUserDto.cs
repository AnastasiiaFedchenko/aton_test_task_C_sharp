using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.DTOs;

public class UpdateUserDto
{
    [RegularExpression(@"^[a-zA-Z�-��-�]+$")]
    public string? Name { get; set; }
    
    [Range(0, 2)]
    public int? Gender { get; set; }
    
    public DateTime? Birthday { get; set; }
}
