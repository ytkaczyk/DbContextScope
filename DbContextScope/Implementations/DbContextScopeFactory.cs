using System;
using System.Data;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class DbContextScopeFactory : IDbContextScopeFactory
  {
    private readonly IAmbientDbContextFactory _ambientDbContextFactory;

    public DbContextScopeFactory(IAmbientDbContextFactory ambientDbContextFactory = null)
    {
      _ambientDbContextFactory = ambientDbContextFactory;
    }

    public IDbContextScope Create(DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting)
    {
      return new DbContextScope(
        joiningOption,
        false,
        null,
        _ambientDbContextFactory);
    }

    public IDbContextReadOnlyScope CreateReadOnly(DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting)
    {
      return new DbContextReadOnlyScope(
        joiningOption,
        null,
        _ambientDbContextFactory);
    }

    public IDbContextScope CreateWithTransaction(IsolationLevel isolationLevel)
    {
      return new DbContextScope(
        DbContextScopeOption.ForceCreateNew,
        false,
        isolationLevel,
        _ambientDbContextFactory);
    }

    public IDbContextReadOnlyScope CreateReadOnlyWithTransaction(IsolationLevel isolationLevel)
    {
      return new DbContextReadOnlyScope(
        DbContextScopeOption.ForceCreateNew,
        isolationLevel,
        _ambientDbContextFactory);
    }

    public IDisposable SuppressAmbientContext()
    {
      return new AmbientContextSuppressor();
    }
  }
}
