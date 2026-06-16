using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.Models;

// IMPORTANT: ViewModel for registration form — never expose User entity directly to views
public class RegisterViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(200, ErrorMessage = "Name must be 200 characters or fewer")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string Email { get; set; } = string.Empty;

    // NOTE: Any non-empty password is accepted per assignment spec (even 1 character)
    [Required(ErrorMessage = "Password is required")]
    [MinLength(1, ErrorMessage = "Password cannot be empty")]
    public string Password { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}

// NOTA BENE: Used for rendering rows in the admin user table
public class UserTableRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }
    public DateTime RegisteredAt { get; set; }
}

// NOTE: Payload for bulk toolbar actions (block/unblock/delete/delete-unverified)
public class BulkActionRequest
{
    public string Action { get; set; } = string.Empty;
    // Selected user IDs from checkboxes
    public List<int> UserIds { get; set; } = new();
}
