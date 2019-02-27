using System.Data;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class DbContextReadOnlyScope : IDbContextReadOnlyScope
  {
    private readonly DbContextScope _internalScope;

    public DbContextReadOnlyScope(DbContextScopeOption joiningOption, IsolationLevel? isolationLevel, IAmbientDbContextFactory ambientDbContextFactory = null)
    {
      _internalScope = new DbContextScope(joiningOption, true, isolationLevel, ambientDbContextFactory);
    }

    public IDbContextCollection DbContexts => _internalScope.DbContexts;

    public void Dispose()
    {
      _internalScope.Dispose();
    }
  }
}
