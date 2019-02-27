using System;
using System.Linq;
using System.Threading.Tasks;
using DbContextScope.Demo.BusinessLogicServices;
using DbContextScope.Demo.CommandModel;
using DbContextScope.Demo.DatabaseContext;
using DbContextScope.Demo.Repositories;
using EntityFrameworkCore.DbContextScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbContextScopeTests
{
  [TestClass]
  public class DemoFixture
  {
    private static ServiceProvider fixtureServiceProvider;
    private IServiceScope _testServiceScope;
    private IServiceProvider _testServiceProvider;

    public TestContext TestContext { get; set; }

    [ClassInitialize]
    public static void ClassSetup(TestContext testContext)
    {
      var services = new ServiceCollection();

      // from library
      services.AddScoped<IAmbientDbContextLocator, AmbientDbContextLocator>();

      // from test
      services.AddScoped<IAmbientDbContextFactory, AmbientDbContextFactory>();
      services.AddScoped<IDbContextScopeFactory, DbContextScopeFactory>();
      services.AddScoped<IUserRepository, UserRepository>();

      services.AddScoped<UserCreationService>();
      services.AddScoped<UserQueryService>();
      services.AddScoped<UserEmailService>();
      services.AddScoped<UserCreditScoreService>();

      services.AddScoped<AmbientDbContextFactoryOptions>();

      fixtureServiceProvider = services.BuildServiceProvider();
    }

    [TestInitialize]
    public void TestSetup()
    {
      _testServiceScope = fixtureServiceProvider.CreateScope();
      _testServiceProvider = _testServiceScope.ServiceProvider;

      var ambientDbContextFactoryOptions = _testServiceProvider.GetRequiredService<AmbientDbContextFactoryOptions>();
      ambientDbContextFactoryOptions.DbContextScopeKey = "DbContextScopeTestDatabase_" + TestContext.FullyQualifiedTestClassName + "::" + TestContext.TestName;
    }

    [TestCleanup]
    public void TestCleanup()
    {
      _testServiceProvider = null;
      _testServiceScope?.Dispose();
      _testServiceScope = null;
    }

    private UserCreationService userCreationService => _testServiceProvider.GetRequiredService<UserCreationService>();
    private UserQueryService userQueryService => _testServiceProvider.GetRequiredService<UserQueryService>();
    private IDbContextScopeFactory dbContextScopeFactory => _testServiceProvider.GetRequiredService<IDbContextScopeFactory>();
    private UserEmailService userEmailService => _testServiceProvider.GetRequiredService<UserEmailService>();

    [TestMethod]
    public void Create_and_retrieve_user_should_work()
    {
      var marysSpec = new UserCreationSpec("Mary", "mary@example.com");

      userCreationService.CreateUser(marysSpec);
      var mary = userQueryService.GetUser(marysSpec.Id);

      Assert.IsNotNull(mary);
    }

    [TestMethod]
    public void Create_list_of_users_and_count_users_should_work()
    {
      var johnSpec = new UserCreationSpec("John", "john@example.com");
      var jeanneSpec = new UserCreationSpec("Jeanne", "jeanne@example.com");
      userCreationService.CreateListOfUsers(johnSpec, jeanneSpec);

      var createdUsers = userQueryService.GetUsers(johnSpec.Id, jeanneSpec.Id);
      
      Assert.AreEqual(2, createdUsers.Count());
    }

    [TestMethod]
    public void Create_list_of_users_in_failing_atomic_transation_should_not_save_anything_at_all()
    {
      var julieSpec = new UserCreationSpec("Julie", "julie@example.com");
      var marcSpec = new UserCreationSpec("Marc", "marc@example.com");

      Assert.ThrowsException<Exception>(() => userCreationService.CreateListOfUsersWithIntentionalFailure(julieSpec, marcSpec));

      var maybeCreatedUsers = userQueryService.GetUsers(julieSpec.Id, marcSpec.Id);

      Assert.AreEqual(0, maybeCreatedUsers.Count());
    }

    [TestMethod]
    public async Task Get_users_in_async_fashion_should_work()
    {
      var johnSpec = new UserCreationSpec("John", "john@example.com");
      var jeanneSpec = new UserCreationSpec("Jeanne", "jeanne@example.com");
      userCreationService.CreateListOfUsers(johnSpec, jeanneSpec);
      
      var usersFoundAsync = await userQueryService.GetTwoUsersAsync(johnSpec.Id, jeanneSpec.Id);

      Assert.AreEqual(2, usersFoundAsync.Count);
    }

    [TestMethod]
    public void Retrieve_user_within_read_uncommited_transaction_should_work()
    {
      var johnSpec = new UserCreationSpec("John", "john@example.com");
      userCreationService.CreateUser(johnSpec);
      
      var userMaybeUncommitted = userQueryService.GetUserUncommitted(johnSpec.Id);

      Assert.IsNotNull(userMaybeUncommitted);
    }

    [TestMethod]
    public void Update_user_in_nested_but_isolated_transaction_should_work()
    {
      var johnSpec = new UserCreationSpec("John", "john@example.com");
      userCreationService.CreateUser(johnSpec);

      using (var parentScope = dbContextScopeFactory.Create())
      {
        var parentDbContext = parentScope.DbContexts.Get<UserManagementDbContext>();

        // Load John in the parent DbContext
        var john = parentDbContext.Users.Find(johnSpec.Id);
        Assert.IsFalse(john.WelcomeEmailSent);

        // Now call our SendWelcomeEmail() business logic service method, which will
        // update John in a non-nested child context
        userEmailService.SendWelcomeEmail(johnSpec.Id);

        Assert.IsTrue(john.WelcomeEmailSent);

        // Note that even though we're not calling SaveChanges() in the parent scope here, the changes
        // made to John by SendWelcomeEmail() will remain persisted in the database as SendWelcomeEmail()
        // forced the creation of a new DbContextScope.
      }
    }

    internal class AmbientDbContextFactoryOptions
    {
      public string DbContextScopeKey { get; set; }
    }

    internal class AmbientDbContextFactory : IAmbientDbContextFactory
    {
      private readonly AmbientDbContextFactoryOptions _options;

      public AmbientDbContextFactory(AmbientDbContextFactoryOptions options)
      {
        _options = options;
      }

      public TDbContext CreateDbContext<TDbContext>() where TDbContext : DbContext
      {
        if (typeof(TDbContext) == typeof(UserManagementDbContext))
        {
          var config = new DbContextOptionsBuilder<UserManagementDbContext>()
                      .UseInMemoryDatabase(_options.DbContextScopeKey)
                      .ConfigureWarnings(warnings => { warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning); });

          return new UserManagementDbContext(config.Options) as TDbContext;
        }

        throw new NotImplementedException(typeof(TDbContext).Name);
      }
    }
  }
}
