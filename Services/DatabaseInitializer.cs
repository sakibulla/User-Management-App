using UserManagementApp.Data;
using Microsoft.EntityFrameworkCore;

namespace UserManagementApp.Services;

/// <summary>
/// IMPORTANT: Helper for initializing the database in production environments.
/// Railway and other platforms may need explicit database setup.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(AppDbContext db)
    {
        try
        {
            // NOTA BENE: Create tables if they don't exist
            await db.Database.EnsureCreatedAsync();
            Console.WriteLine("✓ Database tables created successfully");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"✗ Failed to initialize database: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Check if database is reachable and responsive
    /// </summary>
    public static async Task<bool> CanConnectAsync(AppDbContext db)
    {
        try
        {
            return await db.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}
