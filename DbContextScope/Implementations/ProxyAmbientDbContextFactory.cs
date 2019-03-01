using Castle.DynamicProxy;
using EntityFrameworkCore.DbContextScope.Implementations.Proxy;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class ProxyAmbientDbContextFactory : IAmbientDbContextFactory
  {
    private static readonly ProxyGenerator proxyGenerator = new ProxyGenerator();
    private readonly IAmbientDbContextArgumentFactory _ambientDbContextArgumentFactory;
    private readonly DbContextInterceptorBase _dbContextReadOnlyInterceptor = new DbContextReadonlyInterceptor();
    private readonly DbContextInterceptorBase _dbContextInterceptor = new DbContextInterceptor();

    public ProxyAmbientDbContextFactory(IAmbientDbContextArgumentFactory ambientDbContextArgumentFactory)
    {
      _ambientDbContextArgumentFactory = ambientDbContextArgumentFactory;
    }

    public TDbContext CreateDbContext<TDbContext>(IDbContextScope dbContextScope, bool readOnly) where TDbContext : DbContext
    {
      var interceptor = readOnly
        ? _dbContextReadOnlyInterceptor
        : _dbContextInterceptor;

      var proxyGenerationOptions = new ProxyGenerationOptions(interceptor);
      var constructorArgs = _ambientDbContextArgumentFactory.CreateDbContextArguments<TDbContext>();
      var additionalInterfacesToProxy = new[] { typeof(IDbContextProxyBypass) };
      var proxy = (TDbContext)proxyGenerator.CreateClassProxy(typeof(TDbContext), additionalInterfacesToProxy, proxyGenerationOptions, constructorArgs, interceptor);

      return proxy;
    }
  }
}
