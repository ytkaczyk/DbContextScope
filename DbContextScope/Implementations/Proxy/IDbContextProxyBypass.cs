using System.Threading;
using System.Threading.Tasks;

namespace EntityFrameworkCore.DbContextScope.Implementations.Proxy
{
  internal interface IDbContextProxyBypass
  {
    void DisposeDirect();
    int SaveChangesDirect();
    Task<int> SaveChangesDirectAsync(CancellationToken cancellationToken = default(CancellationToken));
  }
}