using System;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations.Interceptors
{
  internal abstract class DbContextInterceptorBase : IAsyncInterceptor, IProxyGenerationHook
  {
    protected abstract int HandleSaveChanges(IInvocation invocation);

    protected abstract Task<int> HandleSaveChangesAsync(IInvocation invocation);

    protected abstract void HandleDispose(IInvocation invocation);

    void IAsyncInterceptor.InterceptSynchronous(IInvocation invocation)
    {
      if (invocation.Method.Name == nameof(DbContext.Dispose))
      {
        HandleDispose(invocation);

        return;
      }

      if (invocation.Method.Name == nameof(DbContext.SaveChanges))
      {
        invocation.ReturnValue = HandleSaveChanges(invocation);

        return;
      }

      throw new ArgumentOutOfRangeException($"The method '{nameof(DbContext)}.{invocation.Method.Name}' was not chosen to be proxied!");
    }

    void IAsyncInterceptor.InterceptAsynchronous(IInvocation invocation)
    {
      throw new ArgumentOutOfRangeException($"The async method '{nameof(DbContext)}.{invocation.Method.Name}' was not chosen to be proxied!");
    }

    void IAsyncInterceptor.InterceptAsynchronous<TResult>(IInvocation invocation)
    {
      if (invocation.Method.Name == nameof(DbContext.SaveChangesAsync))
      {
        invocation.ReturnValue = HandleSaveChangesAsync(invocation);

        return;
      }

      throw new ArgumentOutOfRangeException($"The async method '{nameof(DbContext)}.{invocation.Method.Name}' was not chosen to be proxied!");
    }

    bool IProxyGenerationHook.ShouldInterceptMethod(Type type, MethodInfo methodInfo)
    {
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
  }
}