using System;
using System.Linq;
using System.Threading.Tasks;
using DbContextScope.Tests.DatabaseContext;
using DbContextScope.Tests.Demo.BusinessLogicServices;
using DbContextScope.Tests.Demo.CommandModel;
using DbContextScope.Tests.Demo.Repositories;
using EntityFrameworkCore.DbContextScope;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbContextScope.Tests.Demo
{
  [TestClass]
  public class DemoFixture : FixtureBase
  {
    protected override void OnTestSetup(ServiceCollection services)
    {
      base.OnTestSetup(services);

      services.AddScoped<IUserRepository, UserRepository>();

      services.AddScoped<UserCreationService>();
      services.AddScoped<UserQueryService>();
      services.AddScoped<UserEmailService>();
      services.AddScoped<UserCreditScoreService>();
    }

    private UserCreationService UserCreationService => TestServiceProvider.GetRequiredService<UserCreationService>();
    private UserQueryService UserQueryService => TestServiceProvider.GetRequiredService<UserQueryService>();
    private IDbContextScopeFactory DBContextScopeFactory => TestServiceProvider.GetRequiredService<IDbContextScopeFactory>();
    private UserEmailService UserEmailService => TestServiceProvider.GetRequiredService<UserEmailService>();
    private UserCreditScoreService UserCreditScoreService => TestServiceProvider.GetRequiredService<UserCreditScoreService>();

    [TestMethod]
    public void Create_and_retrieve_user_should_work()
    {
      var marysSpec = new UserCreationSpec("Mary", "mary@example.com");

      UserCreationService.CreateUser(marysSpec);
      var mary = UserQueryService.GetUser(marysSpec.Id);

      Assert.IsNotNull(mary);
    }

    [TestMethod]
    public void Create_list_of_users_and_count_users_should_work()
    {
      var johnSpec = new UserCreationSpec("John", "john@example.com");
      var jeanneSpec = new UserCreationSpec("Jeanne", "jeanne@example.com");
      UserCreationService.CreateListOfUsers(johnSpec, jeanneSpec);

      var createdUsers = UserQueryService.GetUsers(johnSpec.Id, jeanneSpec.Id);

      Assert.AreEqual(2, createdUsers.Count());
    }

    [TestMethod]
    public void Create_list_of_users_in_failing_atomic_transation_should_not_save_anything_at_all()
    {
      var julieSpec = new UserCreationSpec("Julie", "julie@example.com");
      var marcSpec = new UserCreationSpec("Marc", "marc@example.com");

      Assert.ThrowsException<Exception>(() => UserCreationService.CreateListOfUsersWithIntentionalFailure(julieSpec, marcSpec));

      var maybeCreatedUsers = UserQueryService.GetUsers(julieSpec.Id, marcSpec.Id);

      Assert.AreEqual(0, maybeCreatedUsers.Count());
    }

    [TestMethod]
    public async Task Get_users_in_async_fashion_should_work()
    {
      var johnSpec = new UserCreationSpec("John", "john@example.com");
      var jeanneSpec = new UserCreationSpec("Jeanne", "jeanne@example.com");
      UserCreationService.CreateListOfUsers(johnSpec, jeanneSpec);

      var usersFoundAsync = await UserQueryService.GetTwoUsersAsync(johnSpec.Id, jeanneSpec.Id);

      Assert.AreEqual(2, usersFoundAsync.Count);
    }

    [TestMethod]
    public void Retrieve_user_within_read_uncommited_transaction_should_work()
    {
      var johnSpec = new UserCreationSpec("John", "john@example.com");
      UserCreationService.CreateUser(johnSpec);

      var userMaybeUncommitted = UserQueryService.GetUserUncommitted(johnSpec.Id);

      Assert.IsNotNull(userMaybeUncommitted);
    }

    [TestMethod]
    public void Update_user_in_nested_but_isolated_transaction_should_work()
    {
      var johnSpec = new UserCreationSpec("John", "john@example.com");
      UserCreationService.CreateUser(johnSpec);

      using (var parentScope = DBContextScopeFactory.Create())
      {
        var parentDbContext = parentScope.DbContexts.Get<TestDbContext>();

        // Load John in the parent DbContext
        var john = parentDbContext.Users.Find(johnSpec.Id);
        Assert.IsFalse(john.WelcomeEmailSent);

        // Now call our SendWelcomeEmail() business logic service method, which will
        // update John in a non-nested child context
        UserEmailService.SendWelcomeEmail(johnSpec.Id);

        Assert.IsTrue(john.WelcomeEmailSent);

        // Note that even though we're not calling SaveChanges() in the parent scope here, the changes
        // made to John by SendWelcomeEmail() will remain persisted in the database as SendWelcomeEmail()
        // forced the creation of a new DbContextScope.
      }
    }

    [TestMethod]
    public void Update_user_CreditScore_for_all_users_in_parallel_should_work()
    {
      var johnSpec = new UserCreationSpec("John", "john@example.com");
      var jeanneSpec = new UserCreationSpec("Jeanne", "jeanne@example.com");
      var julieSpec = new UserCreationSpec("Julie", "julie@example.com");
      var marcSpec = new UserCreationSpec("Marc", "marc@example.com");
      var marysSpec = new UserCreationSpec("Mary", "mary@example.com");

      UserCreationService.CreateListOfUsers(johnSpec, jeanneSpec, julieSpec, marcSpec, marysSpec);

      var scoresBefore = UserQueryService.GetUsers(johnSpec.Id, jeanneSpec.Id, julieSpec.Id, marcSpec.Id, marysSpec.Id)
                                         .Select(u => u.CreditScore)
                                         .ToArray();
      Assert.AreEqual(5, scoresBefore.Length);
      Array.ForEach(scoresBefore, s => Assert.AreEqual(0, s));

      UserCreditScoreService.UpdateCreditScoreForAllUsers();

      var scoresAfter = UserQueryService.GetUsers(johnSpec.Id, jeanneSpec.Id, julieSpec.Id, marcSpec.Id, marysSpec.Id)
                                         .Select(u => u.CreditScore)
                                         .ToArray();

      Assert.AreEqual(5, scoresAfter.Length);
      Array.ForEach(scoresAfter, s => Assert.AreNotEqual(0, s));
    }
  }
}
