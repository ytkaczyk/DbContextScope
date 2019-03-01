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
    public void Write_user_should_work()
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
