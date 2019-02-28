using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class DbContextInterceptor : IAsyncInterceptor, IProxyGenerationHook
  {
    // https://github.com/JSkimming/Castle.Core.AsyncInterceptor

    private readonly IDbContextScope _dbContextScope;

    public DbContextInterceptor(IDbContextScope dbContextScope)
    {
      _dbContextScope = dbContextScope;
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
        var dbContext = (DbContext)invocation.Proxy;
        var parentUpdater = new DetectModifiedEntitiesAndUpdateParentScope(dbContext, _dbContextScope);

        var changes = _dbContextScope.SaveChanges();
        parentUpdater.UpdateParent();
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
        var dbContext = (DbContext)invocation.Proxy;
        var parentUpdater = new DetectModifiedEntitiesAndUpdateParentScope(dbContext, _dbContextScope);

        object returnValue;

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

        invocation.ReturnValue = returnValue;

        return;
      }

      throw new NotImplementedException($"The async method '{nameof(DbContext)}.{invocation.Method.Name}' was not chosen to be proxied!");
    }

    private async Task<int> saveChangesAndUpdateParentScopeAsync(DetectModifiedEntitiesAndUpdateParentScope parentUpdater, CancellationToken cancellationToken = default(CancellationToken))
    {
      var changes = await _dbContextScope.SaveChangesAsync(cancellationToken);
      await parentUpdater.UpdateParentAsync(cancellationToken);

      return changes;
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
      //Console.WriteLine("Cant proxy: " + memberInfo.Name);
    }
  }

  internal class DetectModifiedEntitiesAndUpdateParentScope
  {
    private readonly IDbContextScope _dbContextScope;
    private readonly EntityEntry[] _modifiedEntries;

    public DetectModifiedEntitiesAndUpdateParentScope(DbContext dbContext, IDbContextScope dbContextScope)
    {
      _dbContextScope = dbContextScope;
      var changeTracker = dbContext.ChangeTracker;

      changeTracker.DetectChanges();
      if (changeTracker.HasChanges())
      {
        _modifiedEntries = changeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted).ToArray();
      }
    }

    public void UpdateParent()
    {
      if (_modifiedEntries != null)
      {
        _dbContextScope.RefreshEntitiesInParentScope(_modifiedEntries);
      }
    }

    public Task UpdateParentAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
      if (_modifiedEntries != null)
      {
        return _dbContextScope.RefreshEntitiesInParentScopeAsync(_modifiedEntries, cancellationToken);
      }

      return Task.CompletedTask;
    }
  }
}