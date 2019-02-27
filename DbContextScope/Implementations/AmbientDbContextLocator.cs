using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class AmbientDbContextLocator : IAmbientDbContextLocator
  {
    public TDbContext Get<TDbContext>() where TDbContext : DbContext
    {
      var ambientDbContextScope = AmbientContextScopeMagic.GetAmbientScope();

      return ambientDbContextScope?.DbContexts.Get<TDbContext>();
    }
  }
}
