using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DbContextScope.Tests.DatabaseContext
{
  public class DummyDbContext : DbContext
  {
    public DummyDbContext()
    {
        
    }

    public static InMemoryDatabaseRoot GlobalDbRoot = new InMemoryDatabaseRoot();

    public DbSet<DummyEntity> DummyEntities { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      base.OnConfiguring(optionsBuilder);

      optionsBuilder.UseInMemoryDatabase("STATIC_DummyDbContext", GlobalDbRoot);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<DummyEntity>();
    }
  }
}