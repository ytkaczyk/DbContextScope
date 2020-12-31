using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DbContextScope.Tests.DatabaseContext;
using EntityFrameworkCore.DbContextScope;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbContextScope.Tests
{
  [TestClass]
  public class DbContextScopeFixture : FixtureBase, IScopeDiagnostic
  {
    public IDbContextScopeFactory DbContextScopeFactory => TestServiceProvider.GetRequiredService<IDbContextScopeFactory>();
    public IScopeDiagnostic ScopeDiagnostic => TestServiceProvider.GetRequiredService<IScopeDiagnostic>();

    public List<string> CalledMethods { get; } = new List<string>();

    protected override void OnTestSetup(ServiceCollection services)
    {
      base.OnTestSetup(services);

      services.Configure<LoggerFilterOptions>(opts => opts.MinLevel = LogLevel.Trace);
      services.AddScoped<IScopeDiagnostic>(sp => this);
    }

    protected override void OnTestCleanup()
    {
      base.OnTestCleanup();

      DbContextScopeFactory.Dispose();
    }

    [TestMethod]
    public void DbContext_proxy_should_only_be_built_once_in_same_scope()
    {
      // arrange
      TestDbContext dbContext1;
      TestDbContext dbContext2;

      // act
      using (var scope = DbContextScopeFactory.Create())
      {
        dbContext1 = scope.Get<TestDbContext>();
        dbContext2 = scope.Get<TestDbContext>();
      }

      // assert
      Assert.IsTrue(ReferenceEquals(dbContext1, dbContext2), "The db context was created more than once.");
    }

    [TestMethod]
    public void ProxyTypeName_shoud_stay_the_same()
    {
      // arrange
      string dbContext1;
      string dbContext2;

      // act
      using (var scope = DbContextScopeFactory.Create())
      {
        dbContext1 = scope.Get<DummyDbContext>().GetType().Name;
      }

      using (var scope = DbContextScopeFactory.Create())
      {
        dbContext2 = scope.Get<DummyDbContext>().GetType().Name;
      }

      // assert
      Assert.AreEqual(dbContext1, dbContext2);
    }

    [TestMethod]
    public void Write_user_via_scope_should_work()
    {
      // arrange
      var entity = new DummyEntity { Name = "Bob" };

      // act
      using (var scope = DbContextScopeFactory.Create())
      {
        var dbContext = scope.Get<DummyDbContext>();
        dbContext.Add(entity);

        scope.SaveChanges();
      }

      // assert
      using (var scope = DbContextScopeFactory.CreateReadOnly())
      {
        var dbContext = scope.Get<DummyDbContext>();

        var users = dbContext.DummyEntities.ToArray();
        Assert.AreEqual(1, users.Length);
      }
    }

    [TestMethod]
    public void Write_user_via_dbContext_should_work()
    {
      // arrange
      var entity = new DummyEntity { Name = "Bob" };

      // act
      using (var scope = DbContextScopeFactory.Create())
      {
        var dbContext = scope.Get<DummyDbContext>();
        dbContext.Add(entity);

        dbContext.SaveChanges();
      }

      // assert
      using (var scope = DbContextScopeFactory.CreateReadOnly())
      {
        var dbContext = scope.Get<DummyDbContext>();

        var users = dbContext.DummyEntities.ToArray();
        Assert.AreEqual(1, users.Length);
      }
    }

    [TestMethod]
    public void Scope_and_open_and_save_on_open_context_should_make_entity_in_parent_visible()
    {
      // arrange
      var openEntity = new DummyEntity { Name = "Bob" };
      DummyEntity scopeEntity;

      // act
      using (var scope = DbContextScopeFactory.Create())
      {
        var scopeDbContext = scope.Get<DummyDbContext>();

        using (var openDbContext = DbContextScopeFactory.Open<DummyDbContext>())
        {
          openDbContext.Add(openEntity);
          openDbContext.SaveChanges();
        }

        scopeEntity = scopeDbContext.Find<DummyEntity>(openEntity.Id);
      }

      // assert
      Assert.AreEqual(openEntity.Id, scopeEntity.Id);
      DeepComparison.ValidateCompare(openEntity, scopeEntity);
    }

    [TestMethod]
    public void Open_should_create_a_proxy()
    {
      // arrange
      string dummyDbContextTypeName;

      // act
      using (var dummyDbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        dummyDbContextTypeName = dummyDbContext.GetType().Name;
      }

      // assert
      StringAssert.StartsWith(dummyDbContextTypeName, "DummyDbContextProxy");
    }

    [TestMethod]
    public void Dispose_of_proxy_should_be_forwarded()
    {
      // act
      using (DbContextScopeFactory.Open<DummyDbContext>())
      {
      }

      // assert
      assertCallOrder("Dispose");
    }

    [TestMethod]
    public void SaveChanges_of_proxy_should_be_forwarded()
    {
      // act
      using (var dbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        dbContext.SaveChanges();
      }

      // assert
      assertCallOrder("SaveChanges", "Dispose");
    }

    [TestMethod]
    public void SaveChanges_with_bool_of_proxy_should_be_forwarded()
    {
      // act
      using (var dbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        dbContext.SaveChanges(true);
      }

      // assert
      assertCallOrder("SaveChanges", "Dispose");
    }

    [TestMethod]
    public async Task SaveChangesAsync_of_proxy_should_be_forwarded()
    {
      // act
      using (var dbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        await dbContext.SaveChangesAsync();
      }

      // assert
      assertCallOrder("SaveChangesAsync", "Dispose");
    }

    [TestMethod]
    public async Task SaveChangesAsync_with_bool_of_proxy_should_be_forwarded()
    {
      // act
      using (var dbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        await dbContext.SaveChangesAsync(true);
      }

      // assert
      assertCallOrder("SaveChangesAsync", "Dispose");
    }

    [TestMethod]
    public async Task SaveChangesAsync_with_cancellationToken_of_proxy_should_be_forwarded()
    {
      // act
      using (var dbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        await dbContext.SaveChangesAsync(new CancellationToken(false));
      }

      // assert
      assertCallOrder("SaveChangesAsync", "Dispose");
    }

    [TestMethod]
    public async Task SaveChangesAsync_with_bool_and_cancellationToken_of_proxy_should_be_forwarded()
    {
      // act
      using (var dbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        await dbContext.SaveChangesAsync(true, new CancellationToken(false));
      }

      // assert
      assertCallOrder("SaveChangesAsync", "Dispose");
    }

    [TestMethod]
    public void Add_entity_should_call_refresh_entities_in_parent_scope()
    {
      // act
      using (var dbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        dbContext.DummyEntities.Add(new DummyEntity());
        dbContext.SaveChanges();
      }

      // assert
      assertCallOrder("SaveChanges", "RefreshEntitiesInParentScope-SKIP", "Dispose");
    }

    [TestMethod]
    public void Add_nested_entity_should_call_refresh_entities_in_parent_scope()
    {
      // act
      using (var dbContext1 = DbContextScopeFactory.Open<DummyDbContext>())
      {
        using (var dbContext2 = DbContextScopeFactory.Open<DummyDbContext>(DbContextScopeOption.ForceCreateNew))
        {
          dbContext2.DummyEntities.Add(new DummyEntity());
          dbContext2.SaveChanges();
        }

        dbContext1.SaveChanges();
      }

      // assert
      assertCallOrder("SaveChanges", "RefreshEntitiesInParentScope", "Dispose", "SaveChanges", "Dispose");
    }

    [TestMethod]
    public void Update_entity_should_call_refresh_entities_in_parent_scope()
    {
      // arrange
      var dummyEntity = new DummyEntity { Name = "Bob" };
      using (var dbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        dbContext.DummyEntities.Add(dummyEntity);
        dbContext.SaveChanges();
      }
      CalledMethods.Clear();

      // act
      using (var dbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        var savedDummyEntity = dbContext.DummyEntities.Find(dummyEntity.Id);
        savedDummyEntity.Name = "Alice";
        dbContext.SaveChanges();
      }

      // assert
      assertCallOrder("SaveChanges", "RefreshEntitiesInParentScope-SKIP", "Dispose");
    }

    [TestMethod]
    public async Task Add_entity_should_call_refresh_entities_async_in_parent_scope()
    {
      // act
      using (var dbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        await dbContext.DummyEntities.AddAsync(new DummyEntity());
        await dbContext.SaveChangesAsync();
      }

      // assert
      assertCallOrder("SaveChangesAsync", "RefreshEntitiesInParentScopeAsync-SKIP", "Dispose");
    }

    [TestMethod]
    public async Task Add_nested_entity_should_call_refresh_entities_async_in_parent_scope()
    {
      // act
      using (var dbContext1 = DbContextScopeFactory.Open<DummyDbContext>())
      {
        using (var dbContext2 = DbContextScopeFactory.Open<DummyDbContext>(DbContextScopeOption.ForceCreateNew))
        {
          await dbContext2.DummyEntities.AddAsync(new DummyEntity());
          await dbContext2.SaveChangesAsync();
        }
        await dbContext1.SaveChangesAsync();
      }

      // assert
      assertCallOrder("SaveChangesAsync", "RefreshEntitiesInParentScopeAsync", "Dispose", "SaveChangesAsync", "Dispose");
    }

    [TestMethod]
    public async Task Update_entity_should_call_refresh_entities_async_in_parent_scope()
    {
      // arrange
      var dummyEntity = new DummyEntity { Name = "Bob" };
      using (var dbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        await dbContext.DummyEntities.AddAsync(dummyEntity);
        await dbContext.SaveChangesAsync();
      }
      CalledMethods.Clear();

      // act
      using (var dbContext = DbContextScopeFactory.Open<DummyDbContext>())
      {
        var savedDummyEntity = await dbContext.DummyEntities.FindAsync(dummyEntity.Id);
        savedDummyEntity.Name = "Alice";
        await dbContext.SaveChangesAsync();
      }

      // assert
      assertCallOrder("SaveChangesAsync", "RefreshEntitiesInParentScopeAsync-SKIP", "Dispose");
    }

    [TestMethod]
    public void OpenReadOnly_should_create_a_proxy()
    {
      // arrange
      string dummyDbContextTypeName;

      // act
      using (var dummyDbContext = DbContextScopeFactory.OpenReadOnly<DummyDbContext>())
      {
        dummyDbContextTypeName = dummyDbContext.GetType().Name;
      }

      // assert
      StringAssert.StartsWith(dummyDbContextTypeName, "DummyDbContextProxy");
    }

    [TestMethod]
    public void ReadOnly_Dispose_of_proxy_should_be_forwarded()
    {
      // act
      using (DbContextScopeFactory.OpenReadOnly<DummyDbContext>())
      {
      }

      // assert
      assertCallOrder("Dispose");
    }

    [TestMethod]
    public void ReadOnly_SaveChanges_of_proxy_should_be_blocked()
    {
      // act
      using (var dbContext = DbContextScopeFactory.OpenReadOnly<DummyDbContext>())
      {
        Assert.ThrowsException<InvalidOperationException>(() => dbContext.SaveChanges());
      }

      // assert
      assertCallOrder("Dispose");
    }

    [TestMethod]
    public void ReadOnly_SaveChanges_with_bool_of_proxy_should_be_blocked()
    {
      // act
      using (var dbContext = DbContextScopeFactory.OpenReadOnly<DummyDbContext>())
      {
        Assert.ThrowsException<InvalidOperationException>(() => dbContext.SaveChanges(true));
      }

      // assert
      assertCallOrder("Dispose");
    }

    [TestMethod]
    public async Task ReadOnly_SaveChangesAsync_of_proxy_should_be_blocked()
    {
      // act
      using (var dbContext = DbContextScopeFactory.OpenReadOnly<DummyDbContext>())
      {
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync());
      }

      // assert
      assertCallOrder("Dispose");
    }

    [TestMethod]
    public async Task ReadOnly_SaveChangesAsync_with_bool_of_proxy_should_be_blocked()
    {
      // act
      using (var dbContext = DbContextScopeFactory.OpenReadOnly<DummyDbContext>())
      {
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync(true));
      }

      // assert
      assertCallOrder("Dispose");
    }

    [TestMethod]
    public async Task ReadOnly_SaveChangesAsync_with_cancellationToken_of_proxy_should_be_blocked()
    {
      // act
      using (var dbContext = DbContextScopeFactory.OpenReadOnly<DummyDbContext>())
      {
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync(new CancellationToken(false)));
      }

      // assert
      assertCallOrder("Dispose");
    }

    [TestMethod]
    public async Task ReadOnly_SaveChangesAsync_with_bool_and_cancellationToken_of_proxy_should_be_blocked()
    {
      // act
      using (var dbContext = DbContextScopeFactory.OpenReadOnly<DummyDbContext>())
      {
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync(true, new CancellationToken(false)));
      }

      // assert
      assertCallOrder("Dispose");
    }

    private void assertCallOrder(params string[] methods)
    {
      CollectionAssert.AreEquivalent(CalledMethods, methods, $""
                                                           + $"\n\nCalled: {string.Join(", ", CalledMethods)}"
                                                           + $"\n\nExpected: {string.Join(", ", methods)}");
    }
  }
}
