using System.Data;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class DbContextReadOnlyScope : IDbContextReadOnlyScope
  {
    private readonly DbContextScope _internalScope;

    public DbContextReadOnlyScope(DbContextScopeOption joiningOption, IsolationLevel? isolationLevel, IAmbientDbContextFactory ambientDbContextFactory = null)
    {
      _internalScope = new DbContextScope(joiningOption, true, isolationLevel, ambientDbContextFactory);
    }

    public TDbContext Get<TDbContext>() where TDbContext : DbContext
    {
      return _internalScope.Get<TDbContext>();
    }

    public void Dispose()
    {
      _internalScope.Dispose();
    }
  }
}
