using DbContextScope.Tests.Demo.BusinessLogicServices;
using DbContextScope.Tests.Demo.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbContextScope.Tests
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
