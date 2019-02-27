using Microsoft.EntityFrameworkCore;

namespace DbContextScope.Tests.DatabaseContext
{
  public class TestDbContext : DbContext
  {
    public TestDbContext(DbContextOptions options)
      : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Post> Posts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<User>(builder =>
      {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).IsRequired();
        builder.Property(m => m.Email).IsRequired();

        builder.HasMany(m => m.Posts)
               .WithOne(m => m.Author)
               .HasForeignKey(m => m.Id);
      });

      modelBuilder.Entity<Post>(builder =>
      {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).IsRequired();

        builder.HasOne(m => m.Author)
               .WithMany(m => m.Posts)
               .HasForeignKey(m => m.Id)
               .IsRequired();
      });
    }
  }
}
