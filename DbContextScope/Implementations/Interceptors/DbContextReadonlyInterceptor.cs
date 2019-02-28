using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace EntityFrameworkCore.DbContextScope.Implementations.Interceptors
{
  internal class DbContextReadonlyInterceptor : DbContextInterceptorBase
  {
    private readonly IDbContextReadOnlyScope _dbContextScope;

    public DbContextReadonlyInterceptor(IDbContextReadOnlyScope dbContextScope)
    {
      _dbContextScope = dbContextScope;
    }

    protected override void HandleDispose(IInvocation invocation)
    {
      _dbContextScope.Dispose();
    }

    protected override int HandleSaveChanges(IInvocation invocation)
    {
      throw new InvalidOperationException("The DbContext is readonly and doesn't support save changes.");
    }

    protected override Task<int> HandleSaveChangesAsync(IInvocation invocation)
    {
      throw new InvalidOperationException("The DbContext is readonly and doesn't support save changes.");
    }
  }
}