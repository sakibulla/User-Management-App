using Microsoft.EntityFrameworkCore;
using UserManagementApp.Models;

namespace UserManagementApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(u => u.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(320);
            entity.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(u => u.Status).HasColumnName("status").IsRequired().HasMaxLength(20).HasDefaultValue("unverified");
            entity.Property(u => u.RegisteredAt).HasColumnName("registered_at").HasDefaultValueSql("NOW()");
            entity.Property(u => u.LastLoginAt).HasColumnName("last_login_at").IsRequired(false);
            entity.Property(u => u.VerificationToken).HasColumnName("verification_token").IsRequired(false);

            // IMPORTANT: This is the UNIQUE INDEX on email — enforced at the storage level.
            // This is NOT the primary key. It guarantees email uniqueness regardless of
            // how many concurrent sources push data simultaneously (race-condition safe).
            // Per assignment: "YOUR STORAGE SHOULD GUARANTEE E-MAIL UNIQUENESS
            // INDEPENDENTLY OF HOW MANY SOURCES PUSH DATA INTO IT SIMULTANEOUSLY."
            // NOTA BENE: Application code does NOT check for duplicate emails —
            // the database constraint handles it exclusively.
            entity.HasIndex(u => u.Email)
                  .IsUnique()
                  .HasDatabaseName("ix_users_email_unique");
        });
    }
}
