using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Data;
using UserManagementAPI.DTOs;
using UserManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UsersDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UsersDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Create
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _context.Users.AnyAsync(u => u.Login == createDto.Login))
            return BadRequest("User with this login already exists");

        var currentUserLogin = User.Identity?.Name ?? "System";

        var newUser = new User
        {
            Login = createDto.Login,
            Password = createDto.Password,
            Name = createDto.Name,
            Gender = createDto.Gender,
            Birthday = createDto.Birthday,
            Admin = createDto.Admin,
            CreatedBy = currentUserLogin,
            ModifiedBy = currentUserLogin
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"User {newUser.Login} created by {currentUserLogin}");
        return Ok(newUser);
    }
    #endregion

    #region Read
    [HttpGet("active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetActiveUsers()
    {
    	var users = await _context.Users
        	.Where(u => u.RevokedOn == null)
        	.OrderBy(u => u.CreatedOn)
        	.ToListAsync();

    	return Ok(users);
    }

    [HttpGet("by-login/{login}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserByLogin(string login)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Name,
            user.Gender,
            user.Birthday,
            IsActive = user.IsActive,
            Role = user?.Admin == true ? "Admin" : "User"
        });
    }

    [HttpGet("self")]
    public async Task<IActionResult> GetSelf()
    {
        var currentUserLogin = User.Identity?.Name;
        if (currentUserLogin == null) return Unauthorized();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == currentUserLogin);
        if (user == null) return NotFound();
        if (!user.IsActive) return Forbid();

        return Ok(new
        {
            user.Name,
            user.Gender,
            user.Birthday,
            user.Login,
            Role = user.Admin ? "Admin" : "User"
        });
    }

    [HttpGet("older-than/{age}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsersOlderThan(int age)
    {
        if (age <= 0) return BadRequest("Age must be positive");

        var minBirthDate = DateTime.UtcNow.AddYears(-age);
        var users = await _context.Users
            .Where(u => u.Birthday != null && u.Birthday <= minBirthDate)
            .ToListAsync();

        return Ok(users);
    }
    #endregion

    #region Update-1
    [HttpPut("update/{login}")]
    public async Task<IActionResult> UpdateUser(string login, [FromBody] UpdateUserDto updateDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currentUserLogin = User.Identity?.Name;
        if (currentUserLogin == null) return Unauthorized();

        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == currentUserLogin);
        var userToUpdate = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);

        if (userToUpdate == null) return NotFound();
        if (!userToUpdate.IsActive) return BadRequest("User is not active");
        
        if (!User.IsInRole("Admin") && currentUserLogin != login)
            return Forbid();

        if (updateDto.Name != null) userToUpdate.Name = updateDto.Name;
        if (updateDto.Gender.HasValue) userToUpdate.Gender = updateDto.Gender.Value;
        if (updateDto.Birthday.HasValue) userToUpdate.Birthday = updateDto.Birthday.Value;

        userToUpdate.ModifiedOn = DateTime.UtcNow;
        userToUpdate.ModifiedBy = currentUserLogin;

        await _context.SaveChangesAsync();
        _logger.LogInformation($"User {login} updated by {currentUserLogin}");
        return Ok(userToUpdate);
    }

    [HttpPut("change-password/{login}")]
    public async Task<IActionResult> ChangePassword(string login, [FromBody] ChangePasswordDto changeDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currentUserLogin = User.Identity?.Name;
        if (currentUserLogin == null) return Unauthorized();

        var userToUpdate = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        if (userToUpdate == null) return NotFound();
        if (!userToUpdate.IsActive) return BadRequest("User is not active");
        
        if (!User.IsInRole("Admin") && currentUserLogin != login)
            return Forbid();

        userToUpdate.Password = changeDto.NewPassword;
        userToUpdate.ModifiedOn = DateTime.UtcNow;
        userToUpdate.ModifiedBy = currentUserLogin;

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Password for user {login} changed by {currentUserLogin}");
        return Ok();
    }

    [HttpPut("change-login/{login}")]
    public async Task<IActionResult> ChangeLogin(string login, [FromBody] ChangeLoginDto changeDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currentUserLogin = User.Identity?.Name;
        if (currentUserLogin == null) return Unauthorized();

        var userToUpdate = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        if (userToUpdate == null) return NotFound();
        if (!userToUpdate.IsActive) return BadRequest("User is not active");
        
        if (!User.IsInRole("Admin") && currentUserLogin != login)
            return Forbid();

        if (await _context.Users.AnyAsync(u => u.Login == changeDto.NewLogin && u.Id != userToUpdate.Id))
            return BadRequest("New login is already taken");

        userToUpdate.Login = changeDto.NewLogin;
        userToUpdate.ModifiedOn = DateTime.UtcNow;
        userToUpdate.ModifiedBy = currentUserLogin;

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Login changed from {login} to {changeDto.NewLogin} by {currentUserLogin}");
        return Ok();
    }
    #endregion

    #region Delete
    [HttpDelete("{login}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(string login, bool softDelete = true)
    {
    	var currentUserLogin = User.Identity?.Name;
    	if (string.IsNullOrEmpty(currentUserLogin))
            return Unauthorized();

    	var userToDelete = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
    	if (userToDelete == null)
            return NotFound();

    	if (softDelete)
    	{
            userToDelete.RevokedOn = DateTime.UtcNow;
            userToDelete.RevokedBy = currentUserLogin;
            userToDelete.ModifiedOn = DateTime.UtcNow;
            userToDelete.ModifiedBy = currentUserLogin;
    	}
    	else
    	{
            _context.Users.Remove(userToDelete);
    	}

    	try
    	{
            await _context.SaveChangesAsync();
            return Ok();
    	}
    	catch (DbUpdateException ex)
    	{
            _logger.LogError(ex, "Error deleting user");
            return StatusCode(500, "Error deleting user");
    	}
    }
    #endregion

    #region Update-2 (Restore)
    [HttpPut("restore/{login}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RestoreUser(string login)
    {
        var currentUserLogin = User.Identity?.Name;
        if (currentUserLogin == null) return Unauthorized();

        var userToRestore = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        if (userToRestore == null) return NotFound();

        userToRestore.RevokedOn = null;
        userToRestore.RevokedBy = null;
        userToRestore.ModifiedOn = DateTime.UtcNow;
        userToRestore.ModifiedBy = currentUserLogin;

        await _context.SaveChangesAsync();
        _logger.LogInformation($"User {login} restored by {currentUserLogin}");
        return Ok();
    }
    #endregion
}