using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbContextScope.Tests.DatabaseContext
{
  public class BlockingDummyDbContext : DummyDbContext
  {
    public override int SaveChanges()
    {
      throw new AssertFailedException("SaveChanges() is not allowed to be called yet a proxy should intercept it.");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
      throw new AssertFailedException("SaveChanges(bool acceptAllChangesOnSuccess) is not allowed to be called yet a proxy should intercept it.");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
      throw new AssertFailedException("SaveChangesAsync(CancellationToken cancellationToken) is not allowed to be called yet a proxy should intercept it.");
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
      throw new AssertFailedException("SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken) is not allowed to be called yet a proxy should intercept it.");
    }

    public override void Dispose()
    {
      throw new AssertFailedException("Dispose() is not allowed to be called yet a proxy should intercept it.");
    }
  }
}