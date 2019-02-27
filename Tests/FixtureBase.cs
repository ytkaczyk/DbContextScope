using System;
using EntityFrameworkCore.DbContextScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbContextScopeTests
{
  public abstract class FixtureBase
  {
    private ServiceProvider _testContainer;
    private IServiceScope _testServiceScope;
    protected IServiceProvider TestServiceProvider;

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public void TestSetup()
    {
      var services = new ServiceCollection();

      // from library
      services.AddScoped<IAmbientDbContextLocator, AmbientDbContextLocator>();
      services.AddScoped<IDbContextScopeFactory, DbContextScopeFactory>();

      // from fixtureBase
      services.AddScoped<MemoryAmbientDbContextFactoryOptions>();
      services.AddScoped<IAmbientDbContextFactory, MemoryAmbientDbContextFactory>();

      // from test
      OnTestSetup(services);

      _testContainer = services.BuildServiceProvider();

      _testServiceScope = _testContainer.CreateScope();
      TestServiceProvider = _testServiceScope.ServiceProvider;

      var ambientDbContextFactoryOptions = TestServiceProvider.GetRequiredService<MemoryAmbientDbContextFactoryOptions>();
      ambientDbContextFactoryOptions.DbContextScopeKey = $"DbContextScopeTestDatabase_{TestContext.FullyQualifiedTestClassName}::{TestContext.TestName}";
    }

    protected virtual void OnTestSetup(ServiceCollection services)
    {
    }

    [TestCleanup]
    public void TestCleanup()
    {
      TestServiceProvider = null;
      _testServiceScope?.Dispose();
      _testServiceScope = null;
      _testContainer?.Dispose();
      _testContainer = null;
    }

    private class MemoryAmbientDbContextFactoryOptions
    {
      public string DbContextScopeKey { get; set; }
    }

    private class MemoryAmbientDbContextFactory : IAmbientDbContextFactory
    {
      private readonly MemoryAmbientDbContextFactoryOptions _options;

      public MemoryAmbientDbContextFactory(MemoryAmbientDbContextFactoryOptions options)
      {
        _options = options;
      }

      public TDbContext CreateDbContext<TDbContext>() where TDbContext : DbContext
      {
        var config = new DbContextOptionsBuilder()
                    .UseInMemoryDatabase(_options.DbContextScopeKey)
                    .ConfigureWarnings(warnings => { warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning); });

        var instance = Activator.CreateInstance(typeof(TDbContext), config.Options);

        return (TDbContext)instance;
      }
    }
  }
}
