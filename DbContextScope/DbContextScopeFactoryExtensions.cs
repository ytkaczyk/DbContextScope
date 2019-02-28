using System.Data;
using Castle.DynamicProxy;
using EntityFrameworkCore.DbContextScope.Implementations.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope
{
  public static class DbContextScopeFactoryExtensions
  {
    private static readonly ProxyGenerator proxyGenerator = new ProxyGenerator();

    /// <summary>
    /// Creates a new DbContextScope.
    /// By default, the new scope will join the existing ambient scope. This
    /// is what you want in most cases. This ensures that the same DbContext instances
    /// are used by all services methods called within the scope of a business transaction.
    /// Set 'joiningOption' to 'ForceCreateNew' if you want to ignore the ambient scope
    /// and force the creation of new DbContext instances within that scope. Using 'ForceCreateNew'
    /// is an advanced feature that should be used with great care and only if you fully understand the
    /// implications of doing this.
    /// </summary>
    public static TDbContext Open<TDbContext>(this IDbContextScopeFactory self, DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting) where TDbContext : DbContext
    {
      var dbContextScope = self.Create(joiningOption);
      var interceptor = new DbContextInterceptor(dbContextScope);
      var proxy = createDbContextProxy(dbContextScope.Get<TDbContext>(), interceptor, true);

      return proxy;
    }

    /// <summary>
    /// Creates a new DbContextScope for read-only queries.
    /// By default, the new scope will join the existing ambient scope. This
    /// is what you want in most cases. This ensures that the same DbContext instances
    /// are used by all services methods called within the scope of a business transaction.
    /// Set 'joiningOption' to 'ForceCreateNew' if you want to ignore the ambient scope
    /// and force the creation of new DbContext instances within that scope. Using 'ForceCreateNew'
    /// is an advanced feature that should be used with great care and only if you fully understand the
    /// implications of doing this.
    /// </summary>
    public static TDbContext OpenReadOnly<TDbContext>(this IDbContextScopeFactory self, DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting) where TDbContext : DbContext
    {
      var dbContextScope = self.CreateReadOnly(joiningOption);
      var interceptor = new DbContextReadonlyInterceptor(dbContextScope);
      var proxy = createDbContextProxy(dbContextScope.Get<TDbContext>(), interceptor, false);

      return proxy;
    }

    /// <summary>
    /// Forces the creation of a new ambient DbContextScope (i.e. does not
    /// join the ambient scope if there is one) and wraps all DbContext instances
    /// created within that scope in an explicit database transaction with
    /// the provided isolation level.
    /// WARNING: the database transaction will remain open for the whole
    /// duration of the scope! So keep the scope as short-lived as possible.
    /// Don't make any remote API calls or perform any long running computation
    /// within that scope.
    /// This is an advanced feature that you should use very carefully
    /// and only if you fully understand the implications of doing this.
    /// </summary>
    public static TDbContext Open<TDbContext>(this IDbContextScopeFactory self, IsolationLevel isolationLevel) where TDbContext : DbContext
    {
      var dbContextScope = self.CreateWithTransaction(isolationLevel);
      var interceptor = new DbContextInterceptor(dbContextScope);
      var proxy = createDbContextProxy(dbContextScope.Get<TDbContext>(), interceptor, true);

      return proxy;
    }
    
    /// <summary>
    /// Temporarily suppresses the ambient DbContextScope.
    /// Always use this if you need to  kick off parallel tasks within a DbContextScope.
    /// This will prevent the parallel tasks from using the current ambient scope. If you
    /// were to kick off parallel tasks within a DbContextScope without suppressing the ambient
    /// context first, all the parallel tasks would end up using the same ambient DbContextScope, which
    /// would result in multiple threads accesssing the same DbContext instances at the same
    /// time.
    /// </summary>
    public static TDbContext OpenReadOnly<TDbContext>(this IDbContextScopeFactory self, IsolationLevel isolationLevel) where TDbContext : DbContext
    {
      var dbContextScope = self.CreateReadOnlyWithTransaction(isolationLevel);
      var interceptor = new DbContextReadonlyInterceptor(dbContextScope);
      var proxy = createDbContextProxy(dbContextScope.Get<TDbContext>(), interceptor, false);

      return proxy;
    }

    private static TDbContext createDbContextProxy<TDbContext>(TDbContext dbContext, DbContextInterceptorBase interceptor, bool tracking) where TDbContext : DbContext
    {
      /*
       * 1. create DbContextScope
       * 2. create DbContext target
       * 3. create an proxy of TDbContext
       *
       * proxy will do:
       * - intercept SaveChanges/Async
       *   -> track dirty/modified items before Save
       *   -> forward to scope.SaveChanges/Async
       *   -> call RefreshEntitiesInParentScope/Async
       *      
       * - intercept Dispose -> forward scope.Dispose
       */

      var proxyGenerationOptions = new ProxyGenerationOptions(interceptor);
      var proxy = proxyGenerator.CreateClassProxyWithTarget(dbContext, proxyGenerationOptions, interceptor);

      if (!tracking)
      {
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
      }

      return proxy;
    }
  }
}