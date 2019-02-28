using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using EntityFrameworkCore.DbContextScope.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.DbContextScope
{
  public static class DbContextScopeServiceExtensions
  {
    public static IServiceCollection AddDbContextScope(this IServiceCollection self)
    {
      self.AddScoped<IDbContextScopeFactory, DbContextScopeFactory>();
      self.AddScoped<IAmbientDbContextLocator, AmbientDbContextLocator>();

      return self;
    }
  }

  internal class DbContextInterceptor : IAsyncInterceptor, IProxyGenerationHook
  {
    // https://github.com/JSkimming/Castle.Core.AsyncInterceptor

    private readonly IDbContextScope _dbContextScope;
    private Type _dbContextScopeType;

    public DbContextInterceptor(IDbContextScope dbContextScope)
    {
      _dbContextScope = dbContextScope;
      _dbContextScopeType = _dbContextScope.GetType();
    }

    void IAsyncInterceptor.InterceptSynchronous(IInvocation invocation)
    {
      if (invocation.Method.Name == nameof(DbContext.Dispose))
      {
        _dbContextScope.Dispose();

        return;
      }

      if (invocation.Method.Name == nameof(DbContext.SaveChanges))
      {
        var changes = _dbContextScope.SaveChanges();
        invocation.ReturnValue = changes;

        return;
      }

      throw new NotImplementedException($"The method '{nameof(DbContext)}.{invocation.Method.Name}' was not chosen to be proxied!");
    }

    void IAsyncInterceptor.InterceptAsynchronous(IInvocation invocation)
    {
      throw new NotImplementedException($"The async method '{nameof(DbContext)}.{invocation.Method.Name}' was not chosen to be proxied!");
    }

    void IAsyncInterceptor.InterceptAsynchronous<TResult>(IInvocation invocation)
    {
      if (invocation.Method.Name == nameof(DbContext.SaveChangesAsync))
      {
        object returnValue;

        var hasCancellationToken = invocation.Arguments.Any(a => a is CancellationToken);
        if (hasCancellationToken)
        {
          var cancellationToken = (CancellationToken)invocation.Arguments.First(a => a is CancellationToken);
          returnValue = _dbContextScope.SaveChangesAsync(cancellationToken);
        }
        else
        {
          returnValue = _dbContextScope.SaveChangesAsync();
        }

        invocation.ReturnValue = returnValue;

        return;
      }

      throw new NotImplementedException($"The async method '{nameof(DbContext)}.{invocation.Method.Name}' was not chosen to be proxied!");
    }

    bool IProxyGenerationHook.ShouldInterceptMethod(Type type, MethodInfo methodInfo)
    {
      switch (methodInfo.Name)
      {
        case nameof(DbContext.SaveChanges):
        case nameof(DbContext.SaveChangesAsync):
        case nameof(DbContext.Dispose):

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
    }
  }
}
