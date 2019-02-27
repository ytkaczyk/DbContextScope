using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class DbContextScope : IDbContextScope
  {
    private readonly DbContextCollection _dbContexts;
    private readonly bool _nested;
    private readonly DbContextScope _parentScope;
    private readonly bool _readOnly;
    private bool _completed;
    private bool _disposed;

    public DbContextScope(DbContextScopeOption joiningOption, bool readOnly, IsolationLevel? isolationLevel, IAmbientDbContextFactory ambientDbContextFactory = null)
    {
      if (isolationLevel.HasValue && joiningOption == DbContextScopeOption.JoinExisting)
      {
        throw new ArgumentException("Cannot join an ambient DbContextScope when an explicit database transaction "
                                  + "is required. When requiring explicit database transactions to be used (i.e. when the "
                                  + "'isolationLevel' parameter is set), you must not also ask to join the ambient context "
                                  + "(i.e. the 'joinAmbient' parameter must be set to false).");
      }

      _disposed = false;
      _completed = false;
      _readOnly = readOnly;

      _parentScope = AmbientContextScopeMagic.GetAmbientScope();
      if (_parentScope != null && joiningOption == DbContextScopeOption.JoinExisting)
      {
        if (_parentScope._readOnly && !_readOnly)
        {
          throw new InvalidOperationException("Cannot nest a read/write DbContextScope within a read-only DbContextScope.");
        }

        _nested = true;
        _dbContexts = _parentScope._dbContexts;
      }
      else
      {
        _nested = false;
        _dbContexts = new DbContextCollection(readOnly, isolationLevel, ambientDbContextFactory);
      }

      AmbientContextScopeMagic.SetAmbientScope(this);
    }

    public IDbContextCollection DbContexts => _dbContexts;

    public int SaveChanges()
    {
      if (_disposed)
      {
        throw new ObjectDisposedException("DbContextScope");
      }

      if (_completed)
      {
        throw new InvalidOperationException("You cannot call SaveChanges() more than once on a DbContextScope. "
                                          + "A DbContextScope is meant to encapsulate a business transaction: create the "
                                          + "scope at the start of the business transaction and then call SaveChanges() at "
                                          + "the end. Calling SaveChanges() mid-way through a business transaction doesn't "
                                          + "make sense and most likely mean that you should refactor your service method "
                                          + "into two separate service method that each create their own DbContextScope and "
                                          + "each implement a single business transaction.");
      }

      // Only save changes if we're not a nested scope. Otherwise, let the top-level scope 
      // decide when the changes should be saved.
      var c = 0;
      if (!_nested)
      {
        c = commitInternal();
      }

      _completed = true;

      return c;
    }

    public Task<int> SaveChangesAsync()
    {
      return SaveChangesAsync(CancellationToken.None);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancelToken)
    {
      if (cancelToken == null)
      {
        throw new ArgumentNullException(nameof(cancelToken));
      }

      if (_disposed)
      {
        throw new ObjectDisposedException("DbContextScope");
      }

      if (_completed)
      {
        throw new InvalidOperationException("You cannot call SaveChanges() more than once on a DbContextScope. "
                                          + "A DbContextScope is meant to encapsulate a business transaction: create the "
                                          + "scope at the start of the business transaction and then call SaveChanges() at "
                                          + "the end. Calling SaveChanges() mid-way through a business transaction doesn't "
                                          + "make sense and most likely mean that you should refactor your service method "
                                          + "into two separate service method that each create their own DbContextScope and "
                                          + "each implement a single business transaction.");
      }

      // Only save changes if we're not a nested scope. Otherwise, let the top-level scope 
      // decide when the changes should be saved.
      var c = 0;
      if (!_nested)
      {
        c = await commitInternalAsync(cancelToken).ConfigureAwait(false);
      }

      _completed = true;

      return c;
    }

    public void RefreshEntitiesInParentScope(IEnumerable entities)
    {
      if (entities == null)
      {
        return;
      }

      if (_parentScope == null)
      {
        return;
      }

      if (_nested) // The parent scope uses the same DbContext instances as we do - no need to refresh anything
      {
        return;
      }

      // OK, so we must loop through all the DbContext instances in the parent scope
      // and see if their first-level cache (i.e. their ObjectStateManager) contains the provided entities. 
      // If they do, we'll need to force a refresh from the database. 

      // I'm sorry for this code but it's the only way to do this with the current version of Entity Framework 
      // as far as I can see.

      // What would be much nicer would be to have a way to merge all the modified / added / deleted
      // entities from one DbContext instance to another. NHibernate has support for this sort of stuff 
      // but EF still lags behind in this respect. But there is hope: https://entityframework.codeplex.com/workitem/864

      // NOTE: DbContext implements the ObjectContext property of the IObjectContextAdapter interface explicitely.
      // So we must cast the DbContext instances to IObjectContextAdapter in order to access their ObjectContext.
      // This cast is completely safe.

      var entitiesToRefresh = entities as object[] ?? entities.Cast<object>().ToArray();
      foreach (var contextInCurrentScope in _dbContexts.InitializedDbContexts.Values)
      {
        var correspondingParentContext =
          _parentScope._dbContexts.InitializedDbContexts.Values.SingleOrDefault(parentContext => parentContext.GetType() == contextInCurrentScope.GetType());

        if (correspondingParentContext == null)
        {
          continue; // No DbContext of this type has been created in the parent scope yet. So no need to refresh anything for this DbContext type.
        }

        var refreshStrategy = getRefreshStrategy(contextInCurrentScope, correspondingParentContext);
        // Both our scope and the parent scope have an instance of the same DbContext type. 
        // We can now look in the parent DbContext instance for entities that need to
        // be refreshed.
        foreach (var toRefresh in entitiesToRefresh)
        {
          refreshStrategy.Refresh(toRefresh);
        }
      }
    }

    public async Task RefreshEntitiesInParentScopeAsync(IEnumerable entities)
    {
      // See comments in the sync version of this method for an explanation of what we're doing here.

      if (entities == null)
      {
        return;
      }

      if (_parentScope == null)
      {
        return;
      }

      if (_nested)
      {
        return;
      }

      var entitiesToRefresh = entities as object[] ?? entities.Cast<object>().ToArray();
      foreach (var contextInCurrentScope in _dbContexts.InitializedDbContexts.Values)
      {
        var correspondingParentContext =
          _parentScope._dbContexts.InitializedDbContexts.Values.SingleOrDefault(parentContext => parentContext.GetType() == contextInCurrentScope.GetType());

        if (correspondingParentContext == null)
        {
          continue;
        }

        var refreshStrategy = getRefreshStrategy(contextInCurrentScope, correspondingParentContext);
        foreach (var toRefresh in entitiesToRefresh)
        {
          await refreshStrategy.RefreshAsync(toRefresh);
        }
      }
    }

    private static IEntityRefresh getRefreshStrategy(DbContext contextInCurrentScope, DbContext correspondingParentContext)
    {
      return new EntityRefresh(contextInCurrentScope, correspondingParentContext);
    }

    public void Dispose()
    {
      if (_disposed)
      {
        return;
      }

      // Commit / Rollback and dispose all of our DbContext instances
      if (!_nested)
      {
        if (!_completed)
        {
          // Do our best to clean up as much as we can but don't throw here as it's too late anyway.
          try
          {
            if (_readOnly)
            {
              // Disposing a read-only scope before having called its SaveChanges() method
              // is the normal and expected behavior. Read-only scopes get committed automatically.
              commitInternal();
            }
            else
            {
              // Disposing a read/write scope before having called its SaveChanges() method
              // indicates that something went wrong and that all changes should be rolled-back.
              rollbackInternal();
            }
          }
          catch (Exception e)
          {
            Debug.WriteLine(e);
          }

          _completed = true;
        }

        _dbContexts.Dispose();
      }

      // Pop ourself from the ambient scope stack
      var currentAmbientScope = AmbientContextScopeMagic.GetAmbientScope();
      if (currentAmbientScope != this)
      {
        // This is a serious programming error. Worth throwing here.
        throw new InvalidOperationException("DbContextScope instances must be disposed of in the order in which they were created!");
      }

      AmbientContextScopeMagic.RemoveAmbientScope();

      if (_parentScope != null)
      {
        if (_parentScope._disposed)
        {
          /*
           * If our parent scope has been disposed before us, it can only mean one thing:
           * someone started a parallel flow of execution and forgot to suppress the
           * ambient context before doing so. And we've been created in that parallel flow.
           * 
           * Since the CallContext flows through all async points, the ambient scope in the 
           * main flow of execution ended up becoming the ambient scope in this parallel flow
           * of execution as well. So when we were created, we captured it as our "parent scope". 
           * 
           * The main flow of execution then completed while our flow was still ongoing. When 
           * the main flow of execution completed, the ambient scope there (which we think is our 
           * parent scope) got disposed of as it should.
           * 
           * So here we are: our parent scope isn't actually our parent scope. It was the ambient
           * scope in the main flow of execution from which we branched off. We should never have seen 
           * it. Whoever wrote the code that created this parallel task should have suppressed
           * the ambient context before creating the task - that way we wouldn't have captured
           * this bogus parent scope.
           * 
           * While this is definitely a programming error, it's not worth throwing here. We can only 
           * be in one of two scenario:
           * 
           * - If the developer who created the parallel task was mindful to force the creation of 
           * a new scope in the parallel task (with IDbContextScopeFactory.CreateNew() instead of 
           * JoinOrCreate()) then no harm has been done. We haven't tried to access the same DbContext
           * instance from multiple threads.
           * 
           * - If this was not the case, they probably already got an exception complaining about the same
           * DbContext or ObjectContext being accessed from multiple threads simultaneously (or a related
           * error like multiple active result sets on a DataReader, which is caused by attempting to execute
           * several queries in parallel on the same DbContext instance). So the code has already blow up.
           * 
           * So just record a warning here. Hopefully someone will see it and will fix the code.
           */

          var message = @"PROGRAMMING ERROR - When attempting to dispose a DbContextScope, we found that our parent DbContextScope has already been disposed! This means that someone started a parallel flow of execution (e.g. created a TPL task, created a thread or enqueued a work item on the ThreadPool) within the context of a DbContextScope without suppressing the ambient context first. 

In order to fix this:
1) Look at the stack trace below - this is the stack trace of the parallel task in question.
2) Find out where this parallel task was created.
3) Change the code so that the ambient context is suppressed before the parallel task is created. You can do this with IDbContextScopeFactory.SuppressAmbientContext() (wrap the parallel task creation code block in this). 

Stack Trace:
"
                      + Environment.StackTrace;

          Debug.WriteLine(message);
        }
        else
        {
          AmbientContextScopeMagic.SetAmbientScope(_parentScope);
        }
      }

      _disposed = true;
    }

    private int commitInternal()
    {
      return _dbContexts.Commit();
    }

    private Task<int> commitInternalAsync(CancellationToken cancelToken)
    {
      return _dbContexts.CommitAsync(cancelToken);
    }

    private void rollbackInternal()
    {
      _dbContexts.Rollback();
    }

    /*
     * The idea of using an object reference as our instance identifier 
     * instead of simply using a unique string (which we could have generated
     * with Guid.NewGuid() for example) comes from the TransactionScope
     * class. As far as I can make out, a string would have worked just fine.
     * I'm guessing that this is done for optimization purposes. Creating
     * an empty class is cheaper and uses up less memory than generating
     * a unique string.
    */
    internal readonly InstanceIdentifier InstanceIdentifier = new InstanceIdentifier();
  }
}
