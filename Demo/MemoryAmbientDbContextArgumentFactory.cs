using DbContextScope.Demo.DatabaseContext;
using EntityFrameworkCore.DbContextScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using System;

namespace DbContextScope.Demo
{
  internal class MemoryAmbientDbContextArgumentFactory : IAmbientDbContextArgumentFactory
  {
    private readonly InMemoryDatabaseRoot _inMemoryDatabaseRoot;

    public MemoryAmbientDbContextArgumentFactory(
      InMemoryDatabaseRoot inMemoryDatabaseRoot)
    {
      _inMemoryDatabaseRoot = inMemoryDatabaseRoot;
    }

    public object[] CreateDbContextArguments<TDbContext>() where TDbContext : DbContext
    {
      if (typeof(TDbContext) == typeof(UserManagementDbContext))
      {
        var config = new DbContextOptionsBuilder<UserManagementDbContext>()
                    .UseInMemoryDatabase("DbContextScopeTestDatabase", _inMemoryDatabaseRoot)
                    .ConfigureWarnings(warnings => { warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning); });

        return new[] { config.Options };
      }

      throw new ArgumentOutOfRangeException(typeof(TDbContext).Name);
    }
  }
}