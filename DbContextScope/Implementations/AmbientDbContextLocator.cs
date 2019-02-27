using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope
{
  public class AmbientDbContextLocator : IAmbientDbContextLocator
  {
    public TDbContext Get<TDbContext>() where TDbContext : DbContext
    {
      var ambientDbContextScope = AmbientContextScopeMagic.GetAmbientScope();

      return ambientDbContextScope?.DbContexts.Get<TDbContext>();
    }
  }
}
