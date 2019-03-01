using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace EntityFrameworkCore.DbContextScope.Implementations.Proxy
{
  internal class DbContextReadonlyInterceptor : DbContextInterceptorBase
  {
    protected override void OnHandleDispose(IInvocation invocation)
    {
      CurrentDbContextScope.Dispose();
    }

    protected override int OnHandleSaveChanges(IInvocation invocation)
    {
      throw new InvalidOperationException("The DbContext is readonly and doesn't support save changes.");
    }

    protected override Task<int> OnHandleSaveChangesAsync(IInvocation invocation)
    {
      throw new InvalidOperationException("The DbContext is readonly and doesn't support save changes.");
    }

    public override int GetHashCode()
    {
      return typeof(DbContextReadonlyInterceptor).GetHashCode();
    }
  }
}