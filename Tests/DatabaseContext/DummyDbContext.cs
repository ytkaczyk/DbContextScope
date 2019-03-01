using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DbContextScope.Tests.DatabaseContext
{
  public class DummyDbContext : DbContext
  {
    public DummyDbContext()
    {
    }

    public DummyDbContext(DbContextOptions options) : base(options)
    {
    }

    public static InMemoryDatabaseRoot GlobalDbRoot = new InMemoryDatabaseRoot();

    public DbSet<DummyEntity> DummyEntities { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (optionsBuilder.IsConfigured)
      {
        return;
      }

      optionsBuilder.UseInMemoryDatabase("STATIC_DummyDbContext", GlobalDbRoot);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<DummyEntity>();
    }
  }
}