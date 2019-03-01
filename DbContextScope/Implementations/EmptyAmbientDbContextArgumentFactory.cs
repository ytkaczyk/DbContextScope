using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class EmptyAmbientDbContextArgumentFactory : IAmbientDbContextArgumentFactory
  {
    public object[] CreateDbContextArguments<TDbContext>() where TDbContext : DbContext
    {
      return new object[0];
    }
  }
}