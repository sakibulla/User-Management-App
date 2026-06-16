using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserManagementApp.Data;
using UserManagementApp.Models;
using UserManagementApp.Services;

namespace UserManagementApp.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly EmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AppDbContext db, EmailService emailService, ILogger<AccountController> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    // GET /account/login
    [HttpGet]
    public IActionResult Login(string? reason)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Users");

        // NOTE: Show friendly message when redirected due to block/delete
        if (reason == "access_denied")
            TempData["Error"] = "Your account has been blocked or deleted. Please contact an administrator.";

        return View();
    }

    // POST /account/login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // NOTE: Look up user by email; case-insensitive via PostgreSQL ILIKE or EF lower()
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == model.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // IMPORTANT: Blocked users cannot log in — show specific message
        if (user.Status == "blocked")
        {
            ModelState.AddModelError(string.Empty, "Your account has been blocked. Please contact an administrator.");
            return View(model);
        }

        // NOTE: Update last login timestamp before issuing the cookie
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Build claims for cookie authentication
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new("UserId", user.Id.ToString()),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24) });

        return RedirectToAction("Index", "Users");
    }

    // GET /account/register
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Users");

        return View();
    }

    // POST /account/register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // NOTA BENE: We do NOT check for duplicate email in code.
        // The database unique index "ix_users_email_unique" handles this.
        // If a duplicate is inserted concurrently, PostgreSQL throws a unique violation
        // which we catch here and surface as a user-friendly error.
        var token = Guid.NewGuid().ToString("N");

        var user = new User
        {
            Name = model.Name.Trim(),
            Email = model.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Status = "unverified",
            RegisteredAt = DateTime.UtcNow,
            VerificationToken = token,
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ix_users_email_unique") == true
                                        || ex.InnerException?.Message.Contains("duplicate key") == true)
        {
            // IMPORTANT: This is the ONLY place email uniqueness is surfaced to the user.
            // The check is done purely by catching the DB constraint violation.
            ModelState.AddModelError("Email", "An account with this email already exists.");
            return View(model);
        }

        // NOTA BENE: Email is sent ASYNCHRONOUSLY — we do not await it.
        // Registration succeeds immediately; email delivery is best-effort.
        var verificationUrl = Url.Action("Verify", "Account",
            new { token }, Request.Scheme, Request.Host.ToString())!;

        _ = _emailService.SendVerificationEmailAsync(user.Email, user.Name, verificationUrl);

        TempData["Success"] = $"Welcome, {user.Name}! Your account has been created. Check your email to verify your address (optional — you can log in now).";
        return RedirectToAction("Login");
    }

    // GET /account/verify?token=...
    // NOTE: Clicking the email verification link calls this endpoint
    [HttpGet]
    public async Task<IActionResult> Verify(string token)
    {
        if (string.IsNullOrEmpty(token))
            return BadRequest("Invalid verification link.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);

        if (user == null)
        {
            TempData["Error"] = "Verification link is invalid or has already been used.";
            return RedirectToAction("Login");
        }

        // NOTA BENE: Only change status to "active" if not currently blocked.
        // "blocked" status takes precedence and stays "blocked" after verification.
        if (user.Status != "blocked")
            user.Status = "active";

        user.VerificationToken = null; // Invalidate token after use
        await _db.SaveChangesAsync();

        TempData["Success"] = "Email verified successfully! Your account is now active.";
        return RedirectToAction("Login");
    }

    // POST /account/logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
