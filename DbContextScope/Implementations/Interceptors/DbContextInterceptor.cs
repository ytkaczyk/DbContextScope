using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations.Interceptors
{
  internal class DbContextInterceptor : DbContextInterceptorBase
  {
    private readonly IDbContextScope _dbContextScope;

    public DbContextInterceptor(IDbContextScope dbContextScope)
    {
      _dbContextScope = dbContextScope;
    }

    protected override void HandleDispose(IInvocation invocation)
    {
      _dbContextScope.Dispose();
    }

    protected override int HandleSaveChanges(IInvocation invocation)
    {
      var dbContext = (DbContext)invocation.Proxy;
      var parentUpdater = new DetectModifiedEntitiesAndUpdateParentScope(dbContext, _dbContextScope);

      var changes = _dbContextScope.SaveChanges();
      parentUpdater.UpdateParent();

      return changes;
    }

    protected override Task<int> HandleSaveChangesAsync(IInvocation invocation)
    {
      var dbContext = (DbContext)invocation.Proxy;
      var parentUpdater = new DetectModifiedEntitiesAndUpdateParentScope(dbContext, _dbContextScope);

      Task<int> returnValue;

      var hasCancellationToken = invocation.Arguments.Any(a => a is CancellationToken);
      if (hasCancellationToken)
      {
        var cancellationToken = (CancellationToken)invocation.Arguments.First(a => a is CancellationToken);
        returnValue = saveChangesAndUpdateParentScopeAsync(parentUpdater, cancellationToken);
      }
      else
      {
        returnValue = saveChangesAndUpdateParentScopeAsync(parentUpdater);
      }

      return returnValue;
    }

    private async Task<int> saveChangesAndUpdateParentScopeAsync(DetectModifiedEntitiesAndUpdateParentScope parentUpdater, CancellationToken cancellationToken = default(CancellationToken))
    {
      var changes = await _dbContextScope.SaveChangesAsync(cancellationToken);
      await parentUpdater.UpdateParentAsync(cancellationToken);

      return changes;
    }
  }
}