using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Controllers;

// IMPORTANT: All actions in this controller require authentication.
// Non-authenticated users are redirected to /account/login by the cookie auth scheme.
[Authorize]
[Route("users")]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AppDbContext db, ILogger<UsersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET /users — Admin table, sorted by last login time descending (most recent first)
    [HttpGet]
    [Route("")]
    public async Task<IActionResult> Index()
    {
        // NOTA BENE: Data is sorted by LastLoginAt descending per requirement #3.
        // NULLs (never logged in) appear at the bottom.
        var users = await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.LastLoginAt.HasValue)   // non-null first
            .ThenByDescending(u => u.LastLoginAt)             // most recent first
            .ThenBy(u => u.Name)                              // then alphabetically
            .Select(u => new UserTableRow
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Status = u.Status,
                LastLoginAt = u.LastLoginAt,
                RegisteredAt = u.RegisteredAt,
            })
            .ToListAsync();

        return View(users);
    }

    // POST /users/bulk-action — Handles block/unblock/delete/delete-unverified from toolbar
    // IMPORTANT: All users (including the acting user) can be blocked/deleted by any user
    [HttpPost("bulk-action")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAction([FromBody] BulkActionRequest request)
    {
        // NOTE: getUniqIdValue — extracts the current authenticated user's ID from claims
        var currentUserIdStr = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(currentUserIdStr, out var currentUserId))
            return Unauthorized();

        var action = request.Action?.ToLower();
        var ids = request.UserIds ?? new List<int>();

        try
        {
            switch (action)
            {
                // IMPORTANT: Block sets status to "blocked" for selected users
                case "block":
                    if (!ids.Any())
                        return BadRequest(new { message = "No users selected." });

                    await _db.Users
                        .Where(u => ids.Contains(u.Id))
                        .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, "blocked"));

                    // NOTA BENE: If current user blocked themselves, sign them out immediately
                    if (ids.Contains(currentUserId))
                    {
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return Ok(new { message = "Users blocked.", selfAffected = true });
                    }
                    return Ok(new { message = $"{ids.Count} user(s) blocked." });

                // NOTE: Unblock sets status back to "active" (only previously blocked users)
                case "unblock":
                    if (!ids.Any())
                        return BadRequest(new { message = "No users selected." });

                    await _db.Users
                        .Where(u => ids.Contains(u.Id) && u.Status == "blocked")
                        .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, "active"));

                    return Ok(new { message = $"User(s) unblocked." });

                // IMPORTANT: Delete physically removes rows — NOT a soft delete/flag
                // Per spec: "Deleted users should be deleted, not marked"
                case "delete":
                    if (!ids.Any())
                        return BadRequest(new { message = "No users selected." });

                    await _db.Users
                        .Where(u => ids.Contains(u.Id))
                        .ExecuteDeleteAsync();

                    // NOTA BENE: If current user deleted themselves, sign them out
                    if (ids.Contains(currentUserId))
                    {
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return Ok(new { message = "Users deleted.", selfAffected = true });
                    }
                    return Ok(new { message = $"{ids.Count} user(s) deleted." });

                // NOTE: Delete-unverified removes ALL users with status "unverified",
                // regardless of checkbox selection
                case "delete-unverified":
                    var deletedCount = await _db.Users
                        .Where(u => u.Status == "unverified")
                        .ExecuteDeleteAsync();

                    // NOTA BENE: Check if current user was among the deleted unverified users
                    var currentUserExists = await _db.Users.AnyAsync(u => u.Id == currentUserId);
                    if (!currentUserExists)
                    {
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return Ok(new { message = $"{deletedCount} unverified user(s) deleted.", selfAffected = true });
                    }
                    return Ok(new { message = $"{deletedCount} unverified user(s) deleted." });

                default:
                    return BadRequest(new { message = "Unknown action." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BulkAction failed for action={Action}", action);
            return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
        }
    }
}
