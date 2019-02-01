using DbContextScope.Demo.DomainModel;
using Microsoft.EntityFrameworkCore;

namespace DbContextScope.Demo.DatabaseContext
{
  public class UserManagementDbContext : DbContext
  {
    public UserManagementDbContext(DbContextOptions<UserManagementDbContext> options)
      : base(options)
    {
    }

    // Map our 'User' model by convention
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
