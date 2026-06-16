using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementApp.Models;

// IMPORTANT: This model maps directly to the "users" table in PostgreSQL.
// The email uniqueness is enforced at the DATABASE level via a unique index,
// NOT in application code — per assignment requirements.
public class User
{
    // NOTE: Primary key — separate from the unique index on Email
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // IMPORTANT: Email uniqueness is guaranteed by the database unique index
    // (see AppDbContext.OnModelCreating). Application code must NOT check for
    // duplicate emails — the DB constraint handles it.
    [Required]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    // NOTE: Stores bcrypt hash of password, never plaintext
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // Status: "unverified" | "active" | "blocked"
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "unverified";

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    // NOTA BENE: LastLoginAt is updated on every successful login,
    // and is used for sorting the admin table (most recent first)
    public DateTime? LastLoginAt { get; set; }

    // NOTE: Token used for email verification link
    public string? VerificationToken { get; set; }
}
