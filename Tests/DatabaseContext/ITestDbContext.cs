using Microsoft.EntityFrameworkCore;

namespace DbContextScope.Tests.DatabaseContext
{
  public interface ITestDbContext
  {
    DbSet<User> Users { get; set; }
    DbSet<Post> Posts { get; set; }
  }
}