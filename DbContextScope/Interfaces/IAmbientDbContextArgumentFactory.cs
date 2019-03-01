using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope
{
  public interface IAmbientDbContextArgumentFactory
  {
    object[] CreateDbContextArguments<TDbContext>() where TDbContext : DbContext;
  }
}