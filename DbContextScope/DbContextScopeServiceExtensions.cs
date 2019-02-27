using EntityFrameworkCore.DbContextScope.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.DbContextScope
{
  public static class DbContextScopeServiceExtensions
  {
    public static IServiceCollection AddDbContextScope(this IServiceCollection serviceCollection)
    {
      serviceCollection.AddScoped<IDbContextScopeFactory, DbContextScopeFactory>();
      serviceCollection.AddScoped<IAmbientDbContextLocator, AmbientDbContextLocator>();

      return serviceCollection;
    }
  }
}
