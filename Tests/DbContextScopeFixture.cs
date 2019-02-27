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
      var user = new User { Name = "Bob", Email = "bob@local" };

      // act
      using (var scope = DbContextScopeFactory.Create())
      {
        var dbContext = scope.Get<TestDbContext>();
        dbContext.Add(user);
        dbContext.SaveChanges();
      }

      // assert
      using (var scope = DbContextScopeFactory.CreateReadOnly())
      {
        var dbContext = scope.Get<TestDbContext>();

        var users = dbContext.Users.ToArray();
        Assert.AreEqual(1, users.Length);
      }
    }
  }
}
