using DbContextScopeTests.Demo.BusinessLogicServices;
using DbContextScopeTests.Demo.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbContextScopeTests
{
  [TestClass]
  public class DbContextScopeFixture : FixtureBase
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
  }
}
