using System;
using DbContextScope.Demo.DatabaseContext;
using EntityFrameworkCore.DbContextScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DbContextScope.Demo
{
  internal class MemoryAmbientDbContextFactory : IAmbientDbContextFactory
  {
    public TDbContext CreateDbContext<TDbContext>() where TDbContext : DbContext
    {
      if (typeof(TDbContext) == typeof(UserManagementDbContext))
      {
        var config = new DbContextOptionsBuilder<UserManagementDbContext>()
                    .UseInMemoryDatabase("DbContextScopeTestDatabase")
                    .ConfigureWarnings(warnings => { warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning); });

        return new UserManagementDbContext(config.Options) as TDbContext;
      }

      throw new ArgumentOutOfRangeException(typeof(TDbContext).Name);
    }
  }
}