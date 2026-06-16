using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using UserManagementApp.Data;
using Microsoft.EntityFrameworkCore;

namespace UserManagementApp.Middleware;

// IMPORTANT: This middleware runs on EVERY request except registration and login.
// It verifies the authenticated user still exists in DB and is not blocked.
// If the user has been deleted or blocked since their last login, they are
// immediately signed out and redirected to the login page.
// NOTA BENE: This satisfies requirement #5: "before each request except for
// registration or login, server should check if user exists and isn't blocked"
public class UserStatusMiddleware
{
    private readonly RequestDelegate _next;

    // NOTE: Paths that are fully exempt from the user-status check
    private static readonly string[] _exemptPrefixes =
    {
        "/account/login",
        "/account/register",
        "/account/verify",
        "/account/logout",
        "/css",
        "/js",
        "/lib",
        "/favicon",
    };

    public UserStatusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value ?? "";

        // NOTE: Skip the check entirely for exempt paths and unauthenticated users
        var isExempt = _exemptPrefixes.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!isExempt && context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst("UserId")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                // IMPORTANT: Every protected request re-checks DB for fresh user status.
                // This ensures blocked/deleted users are kicked out on their very next request,
                // not just on the next login attempt.
                var user = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                // NOTA BENE: null = deleted user, "blocked" = blocked user — both are rejected
                if (user == null || user.Status == "blocked")
                {
                    await context.SignOutAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme);

                    context.Response.Redirect("/account/login?reason=access_denied");
                    return;
                }
            }
        }

        await _next(context);
    }
}

public static class UserStatusMiddlewareExtensions
{
    public static IApplicationBuilder UseUserStatusCheck(this IApplicationBuilder app)
        => app.UseMiddleware<UserStatusMiddleware>();
}
