using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Middleware;
using UserManagementApp.Services;

var builder = WebApplication.CreateBuilder(args);

// NOTE: Add MVC with views
builder.Services.AddControllersWithViews();

// IMPORTANT: Configure antiforgery token validation.
// For JSON POST requests, the token is sent in the X-CSRF-TOKEN header
// (standard for AJAX/REST APIs).
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// IMPORTANT: Register PostgreSQL database context
// Connection string priority:
// 1. DATABASE_URL environment variable (set by Railway, Heroku, etc.)
// 2. DefaultConnection from appsettings.json (local dev)
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// NOTE: Cookie-based authentication — simple, stateless, no token library needed
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        // NOTA BENE: Cookie name scoped to this app
        options.Cookie.Name = "UserMgmt.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// NOTE: Email service registered as scoped so it gets logger and config injected
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

// IMPORTANT: Only auto-create database in development.
// In production, configure DATABASE_URL environment variable to your remote PostgreSQL instance.
// The connection string is read from appsettings.json or the DATABASE_URL env var.
if (app.Environment.IsDevelopment())
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to create/migrate database on startup. Manual migration may be needed.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();

// IMPORTANT: UserStatusMiddleware must run AFTER UseAuthentication so that
// User.Identity.IsAuthenticated is populated, but BEFORE UseAuthorization
// so blocked/deleted users are kicked out before reaching any controller
app.UseUserStatusCheck();

app.UseAuthorization();

// IMPORTANT: Route pattern supports all controller/action combinations
// - /users → UsersController.Index
// - /users/bulk-action → UsersController.BulkAction
// - /account/login → AccountController.Login (default)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
