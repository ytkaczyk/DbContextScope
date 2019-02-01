using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityFrameworkCore.DbContextScope
{
  internal sealed class EntityRefresh : IEntityRefresh
  {
    private readonly DbContext contextInCurrentScope;
    private readonly DbContext correspondingParentContext;

    public EntityRefresh(DbContext contextInCurrentScope, DbContext correspondingParentContext)
    {
      this.contextInCurrentScope = contextInCurrentScope;
      this.correspondingParentContext = correspondingParentContext;
    }

    public void Refresh<TEntity>(TEntity toRefresh)
    {
      var stateInCurrentScope = getStateInCurrentScope(toRefresh);
      if (stateInCurrentScope != null)
      {
        var stateInParentScope = getStateInParentScope<TEntity>(stateInCurrentScope);
        if (stateInParentScope != null)
        {
          if (shouldRefresh(stateInParentScope))
          {
            correspondingParentContext.Entry(stateInParentScope.Entity).Reload();
          }
        }
      }
    }

    public async Task RefreshAsync<TEntity>(TEntity toRefresh)
    {
      var stateInCurrentScope = getStateInCurrentScope(toRefresh);
      if (stateInCurrentScope != null)
      {
        var stateInParentScope = getStateInParentScope<TEntity>(stateInCurrentScope);
        if (stateInParentScope != null)
        {
          if (shouldRefresh(stateInParentScope))
          {
            await correspondingParentContext.Entry(stateInParentScope.Entity).ReloadAsync();
          }
        }
      }
    }

    private InternalEntityEntry getStateInCurrentScope<TEntity>(TEntity toRefresh)
    {
      // First, we need to find what the EntityKey for this entity is. 
      // We need this EntityKey in order to check if this entity has
      // already been loaded in the parent DbContext's first-level cache (the ObjectStateManager).
      var stateInCurrentScope = contextInCurrentScope.ChangeTracker
                                                     .GetInfrastructure()
                                                     .TryGetEntry(toRefresh);

      return stateInCurrentScope;
    }

    private InternalEntityEntry getStateInParentScope<TEntity>(InternalEntityEntry stateInCurrentScope)
    {
      // NOTE(tim): Thanks to ninety7 (https://github.com/ninety7/DbContextScope) and apawsey (https://github.com/apawsey/DbContextScope)
      // for examples on how identify the matching entities in EF Core.
      var entityType = stateInCurrentScope.Entity.GetType();
      var key = stateInCurrentScope.EntityType.FindPrimaryKey();
      var keyValues = key.Properties
                         .Select(s => entityType.GetProperty(s.Name).GetValue(stateInCurrentScope.Entity))
                         .ToArray();

      // Now we can see if that entity exists in the parent DbContext instance and refresh it.
      var stateInParentScope = correspondingParentContext.ChangeTracker
                                                         .GetInfrastructure()
                                                         .TryGetEntry(key, keyValues);

      return stateInParentScope;
    }

    private static bool shouldRefresh(InternalEntityEntry stateInParentScope)
    {
      // Only refresh the entity in the parent DbContext from the database if that entity hasn't already been
      // modified in the parent. Otherwise, let the whatever concurency rules the application uses apply.

      return stateInParentScope.EntityState == EntityState.Unchanged;
    }
  }
}
