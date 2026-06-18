using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
// 1. DATABASE_URL environment variable (set by Railway) — needs SSL mode conversion
// 2. DefaultConnection from appsettings.json (local dev)

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = databaseUrl ?? builder.Configuration.GetConnectionString("DefaultConnection");

// NOTA BENE: Railway provides DATABASE_URL in postgresql:// format
// We must convert it to NpgsqlConnectionStringBuilder format with SSL mode
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Parse railway's postgresql:// URL and convert to Npgsql format
    var uri = new Uri(databaseUrl);
    var builder_cs = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = uri.UserInfo.Split(':')[0],
        Password = uri.UserInfo.Split(':')[1],
        SslMode = Npgsql.SslMode.Require,
        TrustServerCertificate = true,
        CommandTimeout = 30,
    };
    connectionString = builder_cs.ConnectionString;
}

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

// IMPORTANT: Create/migrate the database tables
// This runs in both Development AND Production on Railway
// Railway's PostgreSQL is ready to accept connections
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        app.Logger.LogInformation("✓ Database tables created/verified successfully");
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "⚠ Failed to initialize database. Continuing anyway.");
    // Don't crash - the database might initialize on first request
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
