using System;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class AmbientDbContextLocator : IAmbientDbContextLocator
  {
    public TDbContext Get<TDbContext>() where TDbContext : DbContext
    {
      var ambientDbContextScope = AmbientContextScopeMagic.GetAmbientScope();

      if (ambientDbContextScope == null)
      {
        throw new InvalidOperationException("No open ambient DbContextScope was found.");
      }

      return ambientDbContextScope.Get<TDbContext>();
    }
  }
}
