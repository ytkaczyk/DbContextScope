using System.Data;

namespace EntityFrameworkCore.DbContextScope
{
  public class DbContextReadOnlyScope : IDbContextReadOnlyScope
  {
    private readonly DbContextScope _internalScope;

    public DbContextReadOnlyScope(IDbContextFactory dbContextFactory = null)
      : this(DbContextScopeOption.JoinExisting, null, dbContextFactory)
    {
    }

    public DbContextReadOnlyScope(IsolationLevel isolationLevel, IDbContextFactory dbContextFactory = null)
      : this(DbContextScopeOption.ForceCreateNew, isolationLevel, dbContextFactory)
    {
    }

    public DbContextReadOnlyScope(DbContextScopeOption joiningOption, IsolationLevel? isolationLevel, IDbContextFactory dbContextFactory = null)
    {
      _internalScope = new DbContextScope(joiningOption, true, isolationLevel, dbContextFactory);
    }

    public IDbContextCollection DbContexts => _internalScope.DbContexts;

    public void Dispose()
    {
      _internalScope.Dispose();
    }
  }
}
