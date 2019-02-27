using DbContextScopeTests.Demo.DomainModel;
using Microsoft.EntityFrameworkCore;

namespace DbContextScopeTests.Demo.DatabaseContext
{
  public class UserManagementDbContext : DbContext
  {
    public UserManagementDbContext(DbContextOptions options)
      : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<User>(builder =>
      {
        builder.Property(m => m.Name).IsRequired();
        builder.Property(m => m.Email).IsRequired();
      });
    }
  }
}
