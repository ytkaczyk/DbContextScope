using Microsoft.EntityFrameworkCore;

namespace DbContextScope.Tests.DatabaseContext
{
  public class DummyDbContext : DbContext
  {
    public DummyDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<DummyEntity> DummyEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<DummyEntity>();
    }
  }
}