using Castle.DynamicProxy;
using EntityFrameworkCore.DbContextScope.Implementations;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope
{
  public static class DbContextScopeFactoryExtensions
  {
    private static readonly ProxyGenerator proxyGenerator = new ProxyGenerator();

    /* TODO
     * Create(DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting)
     * CreateReadOnly(DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting)
     * CreateWithTransaction(IsolationLevel isolationLevel)
     * CreateReadOnlyWithTransaction(IsolationLevel isolationLevel)
     *
     * readonly: ((DbContext)invocation.InvocationTarget).ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking
     */
    public static TDbContext Open<TDbContext>(this IDbContextScopeFactory self, DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting) where TDbContext : DbContext
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

      var dbContextScope = self.Create(joiningOption);
      var dbContext = dbContextScope.Get<TDbContext>();
      var dbContextInterceptor = new DbContextInterceptor(dbContextScope);
      var proxyGenerationOptions = new ProxyGenerationOptions(dbContextInterceptor);
      var proxy = proxyGenerator.CreateClassProxyWithTarget(dbContext, proxyGenerationOptions, dbContextInterceptor);

      return proxy;
    }
  }
}