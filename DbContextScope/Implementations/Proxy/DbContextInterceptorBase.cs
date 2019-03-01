using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations.Proxy
{
  internal abstract class DbContextInterceptorBase : IAsyncInterceptor, IProxyGenerationHook
  {
    protected virtual void HandleSaveChanges(IInvocation invocation)
    {
      if (CallContext.IsInScope("BypassSaveChanges"))
      {
        invocation.Proceed();
      }
      else
      {
        invocation.ReturnValue = OnHandleSaveChanges(invocation);
      }
    }

    protected virtual void HandleSaveChangesAsync(IInvocation invocation)
    {
      if (CallContext.IsInScope("BypassSaveChangesAsync"))
      {
        invocation.Proceed();
      }
      else
      {
        invocation.ReturnValue = OnHandleSaveChangesAsync(invocation);
      }
    }

    protected virtual void HandleDispose(IInvocation invocation)
    {
      if (CallContext.IsInScope("BypassDispose"))
      {
        invocation.Proceed();
      }
      else
      {
        OnHandleDispose(invocation);
      }
    }

    protected abstract int OnHandleSaveChanges(IInvocation invocation);

    protected abstract Task<int> OnHandleSaveChangesAsync(IInvocation invocation);

    protected abstract void OnHandleDispose(IInvocation invocation);

    private void HandleDisposeDirect(IInvocation invocation)
    {
      using (CallContext.OpenScope("BypassDispose"))
      {
        var dbContext = (DbContext)invocation.Proxy;
        dbContext.Dispose();
      }
    }

    private int HandleSaveChangesDirect(IInvocation invocation)
    {
      using (CallContext.OpenScope("BypassSaveChanges"))
      {
        var dbContext = (DbContext)invocation.Proxy;
        var changes = dbContext.SaveChanges();

        return changes;
      }
    }

    private Task<int> HandleSaveChangesDirectAsync(IInvocation invocation)
    {
      using (CallContext.OpenScope("BypassSaveChangesAsync"))
      {
        var maybeCancellationToken = GetCancellationTokenFromArgs(invocation);
        var dbContext = (DbContext)invocation.Proxy;

        return maybeCancellationToken.HasValue
          ? dbContext.SaveChangesAsync(maybeCancellationToken.Value)
          : dbContext.SaveChangesAsync();
      }
    }

    void IAsyncInterceptor.InterceptSynchronous(IInvocation invocation)
    {
      switch (invocation.Method.Name)
      {
        case nameof(DbContext.Dispose):
          HandleDispose(invocation);

          return;
        case nameof(IDbContextProxyBypass.DisposeDirect):
          HandleDisposeDirect(invocation);

          return;
        case nameof(DbContext.SaveChanges):
          HandleSaveChanges(invocation);

          return;
        case nameof(IDbContextProxyBypass.SaveChangesDirect):
          invocation.ReturnValue = HandleSaveChangesDirect(invocation);

          return;
        default:

          throw new ArgumentOutOfRangeException($"The method '{nameof(DbContext)}.{invocation.Method.Name}' was not chosen to be proxied!");
      }
    }

    void IAsyncInterceptor.InterceptAsynchronous(IInvocation invocation)
    {
      throw new ArgumentOutOfRangeException($"The async method '{nameof(DbContext)}.{invocation.Method.Name}' was not chosen to be proxied!");
    }

    void IAsyncInterceptor.InterceptAsynchronous<TResult>(IInvocation invocation)
    {
      switch (invocation.Method.Name)
      {
        case nameof(DbContext.SaveChangesAsync):
          HandleSaveChangesAsync(invocation);

          return;
        case nameof(IDbContextProxyBypass.SaveChangesDirectAsync):
          invocation.ReturnValue = HandleSaveChangesDirectAsync(invocation);

          return;
        default:

          throw new ArgumentOutOfRangeException($"The async method '{nameof(DbContext)}.{invocation.Method.Name}' was not chosen to be proxied!");
      }
    }

    protected CancellationToken? GetCancellationTokenFromArgs(IInvocation invocation)
    {
      var hasCancellationToken = invocation.Arguments.Any(a => a is CancellationToken);
      if (hasCancellationToken)
      {
        var cancellationToken = (CancellationToken)invocation.Arguments.First(a => a is CancellationToken);

        return cancellationToken;
      }

      return null;
    }

    bool IProxyGenerationHook.ShouldInterceptMethod(Type type, MethodInfo methodInfo)
    {
      if (type == typeof(IDbContextProxyBypass))
      {
        return true;
      }

      switch (methodInfo.Name)
      {
        case nameof(DbContext.Dispose):
        case nameof(DbContext.SaveChanges):
        case nameof(DbContext.SaveChangesAsync):

          return true;
        default:

          return false;
      }
    }

    void IProxyGenerationHook.MethodsInspected()
    {
    }

    void IProxyGenerationHook.NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
    {
      //Console.WriteLine("Cant proxy: " + memberInfo.Name);
    }

    public abstract override int GetHashCode();

    public override bool Equals(object obj)
    {
      return GetHashCode() == obj?.GetHashCode();
    }
  }
}