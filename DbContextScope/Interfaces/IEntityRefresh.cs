using System.Threading;
using System.Threading.Tasks;

namespace EntityFrameworkCore.DbContextScope
{
  internal interface IEntityRefresh
  {
    void Refresh<TEntity>(TEntity toRefresh);

    Task RefreshAsync<TEntity>(TEntity toRefresh, CancellationToken cancellationToken = default(CancellationToken));
  }
}
