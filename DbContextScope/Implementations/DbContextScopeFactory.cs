using System;
using System.Data;

namespace EntityFrameworkCore.DbContextScope
{
  public class DbContextScopeFactory : IDbContextScopeFactory
  {
    private readonly IDbContextFactory _dbContextFactory;

    public DbContextScopeFactory(IDbContextFactory dbContextFactory = null)
    {
      _dbContextFactory = dbContextFactory;
    }

    public IDbContextScope Create(DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting)
    {
      return new DbContextScope(
        joiningOption,
        false,
        null,
        _dbContextFactory);
    }

    public IDbContextReadOnlyScope CreateReadOnly(DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting)
    {
      return new DbContextReadOnlyScope(
        joiningOption,
        null,
        _dbContextFactory);
    }

    public IDbContextScope CreateWithTransaction(IsolationLevel isolationLevel)
    {
      return new DbContextScope(
        DbContextScopeOption.ForceCreateNew,
        false,
        isolationLevel,
        _dbContextFactory);
    }

    public IDbContextReadOnlyScope CreateReadOnlyWithTransaction(IsolationLevel isolationLevel)
    {
      return new DbContextReadOnlyScope(
        DbContextScopeOption.ForceCreateNew,
        isolationLevel,
        _dbContextFactory);
    }

    public IDisposable SuppressAmbientContext()
    {
      return new AmbientContextSuppressor();
    }
  }
}
