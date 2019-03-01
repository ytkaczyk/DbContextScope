using EntityFrameworkCore.DbContextScope.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.DbContextScope
{
  public static class DbContextScopeServiceExtensions
  {
    public static IServiceCollection AddDbContextScope(this IServiceCollection self)
    {
      self.AddScoped<IDbContextScopeFactory, DbContextScopeFactory>();
      self.AddScoped<IAmbientDbContextLocator, AmbientDbContextLocator>();
      self.AddScoped<IAmbientDbContextFactory, ProxyAmbientDbContextFactory>();

      self.AddLogging();

      return self;
    }
  }
}
