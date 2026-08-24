using Microsoft.EntityFrameworkCore;
using UserAuth.Api.Models;

namespace UserAuth.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            entity.Property(u => u.MobileNumber).IsRequired().HasMaxLength(20);
            entity.Property(u => u.LandPhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(u => u.Address).IsRequired().HasMaxLength(500);
            entity.Property(u => u.DateOfBirth).IsRequired();
            entity.Property(u => u.Gender).IsRequired().HasMaxLength(20);
            entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
            entity.Property(u => u.CreatedAt).IsRequired();
        });
    }
}
