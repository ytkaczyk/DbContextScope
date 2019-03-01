using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations.Proxy
{
  internal class DbContextInterceptor : DbContextInterceptorBase
  {

    protected override void OnHandleDispose(IInvocation invocation)
    {
      CurrentDbContextScope.Dispose();
    }

    protected override int OnHandleSaveChanges(IInvocation invocation)
    {
      var dbContext = (DbContext)invocation.Proxy;
      var parentUpdater = new DetectModifiedEntitiesAndUpdateParentScope(dbContext, CurrentDbContextScope);

      var changes = CurrentDbContextScope.SaveChanges();
      parentUpdater.UpdateParent();

      return changes;
    }

    protected override Task<int> OnHandleSaveChangesAsync(IInvocation invocation)
    {
      var dbContext = (DbContext)invocation.Proxy;
      var parentUpdater = new DetectModifiedEntitiesAndUpdateParentScope(dbContext, CurrentDbContextScope);

      Task<int> returnValue;

      var maybeCancellationToken = GetCancellationTokenFromArgs(invocation);
      if (maybeCancellationToken.HasValue)
      {
        returnValue = saveChangesAndUpdateParentScopeAsync(parentUpdater, maybeCancellationToken.Value);
      }
      else
      {
        returnValue = saveChangesAndUpdateParentScopeAsync(parentUpdater);
      }

      return returnValue;
    }

    private async Task<int> saveChangesAndUpdateParentScopeAsync(DetectModifiedEntitiesAndUpdateParentScope parentUpdater, CancellationToken cancellationToken = default(CancellationToken))
    {
      var changes = await CurrentDbContextScope.SaveChangesAsync(cancellationToken);
      await parentUpdater.UpdateParentAsync(cancellationToken);

      return changes;
    }

    public override int GetHashCode()
    {
      return typeof(DbContextInterceptor).GetHashCode();
    }
  }
}