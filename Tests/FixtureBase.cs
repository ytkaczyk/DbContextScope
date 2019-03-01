using System;
using EntityFrameworkCore.DbContextScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbContextScope.Tests
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
      services.AddDbContextScope();

      // from fixtureBase
      services.AddScoped<MemoryAmbientDbContextFactoryOptions>();
      services.AddScoped<IAmbientDbContextArgumentFactory, MemoryAmbientDbContextArgumentFactory>();

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
      OnTestCleanup();

      TestServiceProvider = null;
      _testServiceScope?.Dispose();
      _testServiceScope = null;
      _testContainer?.Dispose();
      _testContainer = null;
    }

    protected virtual void OnTestCleanup()
    {
    }

    private class MemoryAmbientDbContextFactoryOptions
    {
      public string DbContextScopeKey { get; set; }
    }

    private class MemoryAmbientDbContextArgumentFactory : IAmbientDbContextArgumentFactory
    {
      private readonly MemoryAmbientDbContextFactoryOptions _options;

      public MemoryAmbientDbContextArgumentFactory(MemoryAmbientDbContextFactoryOptions options)
      {
        _options = options;
      }

      public object[] CreateDbContextArguments<TDbContext>() where TDbContext : DbContext
      {
        var config = new DbContextOptionsBuilder()
                    .UseInMemoryDatabase(_options.DbContextScopeKey, globalDbRoot)
                    .ConfigureWarnings(warnings => { warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning); });

        return new object[] { config.Options };
      }

      private static readonly InMemoryDatabaseRoot globalDbRoot = new InMemoryDatabaseRoot();
    }
  }
}
