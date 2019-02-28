using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityFrameworkCore.DbContextScope.Implementations.Interceptors
{
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