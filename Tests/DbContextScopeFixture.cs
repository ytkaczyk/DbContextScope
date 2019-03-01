using System.Linq;
using DbContextScope.Tests.DatabaseContext;
using EntityFrameworkCore.DbContextScope;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbContextScope.Tests
{
  [TestClass]
  public class DbContextScopeFixture : FixtureBase
  {
    public IDbContextScopeFactory DbContextScopeFactory => TestServiceProvider.GetRequiredService<IDbContextScopeFactory>();

    protected override void OnTestSetup(ServiceCollection services)
    {
      base.OnTestSetup(services);
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
      Assert.IsTrue(object.ReferenceEquals(dbContext1, dbContext2), "The db context was created more than once.");
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

    [TestMethod, Ignore("fixme")]
    public void Scope_and_open_and_save_on_open_context_should_make_entity_in_parent_visible()
    {
      // arrange
      var openEntity = new DummyEntity { Name = "Bob" };
      DummyEntity scopeEntity;

      /*
       * PROBLEM here:
       * - the inner dbContext is joining the outer dbContext
       * - the tracked dbContext in the outer/parent is not the proxy, thus, no updates were populated.
       * -> maybe the proxy-dbContexts may never "join" the parent scope?
       * -> maybe we should replace or stack the joined dbScopes?
       * --> who is disposing the inner-most dbContext then and how?
       */

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
    }
  }
}
