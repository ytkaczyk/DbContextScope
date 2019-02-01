using System.Data;

namespace EntityFrameworkCore.DbContextScope
{
  public class DbContextReadOnlyScope : IDbContextReadOnlyScope
  {
    private readonly DbContextScope _internalScope;

    public DbContextReadOnlyScope(IAmbientDbContextFactory ambientDbContextFactory = null)
      : this(DbContextScopeOption.JoinExisting, null, ambientDbContextFactory)
    {
    }

    public DbContextReadOnlyScope(IsolationLevel isolationLevel, IAmbientDbContextFactory ambientDbContextFactory = null)
      : this(DbContextScopeOption.ForceCreateNew, isolationLevel, ambientDbContextFactory)
    {
    }

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
