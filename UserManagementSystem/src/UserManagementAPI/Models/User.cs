using System;
using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Gender { get; set; } // 0 - женщина, 1 - мужчина, 2 - неизвестно
    public DateTime? Birthday { get; set; }
    public bool Admin { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime ModifiedOn { get; set; } = DateTime.UtcNow;
    public string ModifiedBy { get; set; } = string.Empty;
    public DateTime? RevokedOn { get; set; }
    public string RevokedBy { get; set; } = string.Empty;
    
    public bool IsActive => RevokedOn == null;
}
