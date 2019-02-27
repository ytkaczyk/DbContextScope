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
      services.AddScoped<AmbientDbContextFactoryOptions>();
      services.AddScoped<IAmbientDbContextFactory, AmbientDbContextFactory>();

      // from test
      OnTestSetup(services);

      _testContainer = services.BuildServiceProvider();

      _testServiceScope = _testContainer.CreateScope();
      TestServiceProvider = _testServiceScope.ServiceProvider;

      var ambientDbContextFactoryOptions = TestServiceProvider.GetRequiredService<AmbientDbContextFactoryOptions>();
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

    private class AmbientDbContextFactoryOptions
    {
      public string DbContextScopeKey { get; set; }
    }

    private class AmbientDbContextFactory : IAmbientDbContextFactory
    {
      private readonly AmbientDbContextFactoryOptions _options;

      public AmbientDbContextFactory(AmbientDbContextFactoryOptions options)
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
