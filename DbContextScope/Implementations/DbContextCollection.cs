using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.DbContextScope.Implementations.Proxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  /// <summary>
  /// As its name suggests, DbContextCollection maintains a collection of DbContext instances.
  /// What it does in a nutshell:
  /// - Lazily instantiates DbContext instances when its Get Of TDbContext () method is called
  /// (and optionally starts an explicit database transaction).
  /// - Keeps track of the DbContext instances it created so that it can return the existing
  /// instance when asked for a DbContext of a specific type.
  /// - Takes care of committing / rolling back changes and transactions on all the DbContext
  /// instances it created when its Commit() or Rollback() method is called.
  /// </summary>
  internal class DbContextCollection : IDbContextCollection
  {
    private readonly IAmbientDbContextFactory _ambientDbContextFactory;
    private readonly IsolationLevel? _isolationLevel;
    private readonly DbContextScope _dbContextScope;
    private readonly bool _readOnly;
    private readonly Dictionary<DbContext, IDbContextTransaction> _transactions;
    private bool _completed;
    private bool _disposed;
    private ILogger<DbContextCollection> _logger;

    public DbContextCollection(DbContextScope dbContextScope, IAmbientDbContextFactory ambientDbContextFactory, ILoggerFactory loggerFactory, bool readOnly, IsolationLevel? isolationLevel)
    {
      _disposed = false;
      _completed = false;
      _logger = loggerFactory.CreateLogger<DbContextCollection>();

      InitializedDbContexts = new Dictionary<Type, (DbContext DbContext, IDbContextProxyBypass Proxy)>();
      _transactions = new Dictionary<DbContext, IDbContextTransaction>();

      _dbContextScope = dbContextScope;
      _readOnly = readOnly;
      _isolationLevel = isolationLevel;
      _ambientDbContextFactory = ambientDbContextFactory;
    }

    internal Dictionary<Type, (DbContext DbContext, IDbContextProxyBypass Proxy)> InitializedDbContexts { get; }

    public TDbContext GetOrCreate<TDbContext>() where TDbContext : DbContext
    {
      if (_disposed)
      {
        throw new ObjectDisposedException("DbContextCollection");
      }

      var requestedType = typeof(TDbContext);

      if (!InitializedDbContexts.ContainsKey(requestedType))
      {
        // First time we've been asked for this particular DbContext type.
        // Create one, cache it and start its database transaction if needed.
        var dbContext = _ambientDbContextFactory.CreateDbContext<TDbContext>(_dbContextScope, _readOnly);

        InitializedDbContexts.Add(requestedType, (dbContext, (IDbContextProxyBypass)dbContext));

        if (_readOnly)
        {
          dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        }

        if (_isolationLevel.HasValue)
        {
          var tran = dbContext.Database.BeginTransaction(_isolationLevel.Value);
          _transactions.Add(dbContext, tran);
        }
      }

      return InitializedDbContexts[requestedType].DbContext as TDbContext;
    }

    public void Dispose()
    {
      if (_disposed)
      {
        return;
      }

      // Do our best here to dispose as much as we can even if we get errors along the way.
      // Now is not the time to throw. Correctly implemented applications will have called
      // either Commit() or Rollback() first and would have got the error there.

      if (!_completed)
      {
        try
        {
          if (_readOnly)
          {
            Commit();
          }
          else
          {
            Rollback();
          }
        }
        catch (Exception e)
        {
          _logger.LogError(e, "Error while disposing DbContextCollection.");
          // TODO: throw exception?
        }
      }

      foreach (var item in InitializedDbContexts)
      {
        try
        {
          item.Value.Proxy.DisposeDirect();
        }
        catch (Exception e)
        {
          _logger.LogError(e, $"Error while disposing DbContext '{item.Key.FullName}'.");
          // TODO: throw exception?
        }
      }

      InitializedDbContexts.Clear();
      _disposed = true;
    }

    public int Commit()
    {
      if (_disposed)
      {
        throw new ObjectDisposedException("DbContextCollection");
      }

      if (_completed)
      {
        throw new InvalidOperationException("You can't call Commit() or Rollback() more than once on a DbContextCollection. All the changes in the DbContext instances managed by this collection have already been saved or rollback and all database transactions have been completed and closed. If you wish to make more data changes, create a new DbContextCollection and make your changes there.");
      }

      // Best effort. You'll note that we're not actually implementing an atomic commit 
      // here. It entirely possible that one DbContext instance will be committed successfully
      // and another will fail. Implementing an atomic commit would require us to wrap
      // all of this in a TransactionScope. The problem with TransactionScope is that 
      // the database transaction it creates may be automatically promoted to a 
      // distributed transaction if our DbContext instances happen to be using different 
      // databases. And that would require the DTC service (Distributed Transaction Coordinator)
      // to be enabled on all of our live and dev servers as well as on all of our dev workstations.
      // Otherwise the whole thing would blow up at runtime. 

      // In practice, if our services are implemented following a reasonably DDD approach,
      // a business transaction (i.e. a service method) should only modify entities in a single
      // DbContext. So we should never find ourselves in a situation where two DbContext instances
      // contain uncommitted changes here. We should therefore never be in a situation where the below
      // would result in a partial commit. 

      ExceptionDispatchInfo lastError = null;

      var c = 0;

      foreach (var dbContext in InitializedDbContexts.Values)
      {
        try
        {
          if (!_readOnly)
          {
            c += dbContext.Proxy.SaveChangesDirect();
          }

          // If we've started an explicit database transaction, time to commit it now.
          var tran = getValueOrDefault(_transactions, dbContext.DbContext);
          if (tran != null)
          {
            tran.Commit();
            tran.Dispose();
          }
        }
        catch (Exception e)
        {
          lastError = ExceptionDispatchInfo.Capture(e);
        }
      }

      _transactions.Clear();
      _completed = true;

      if (lastError != null)
      {
        // Re-throw while maintaining the exception's original stack track
        lastError.Throw();
      }

      return c;
    }

    public Task<int> CommitAsync()
    {
      return CommitAsync(CancellationToken.None);
    }

    public async Task<int> CommitAsync(CancellationToken cancelToken)
    {
      if (cancelToken == null)
      {
        throw new ArgumentNullException(nameof(cancelToken));
      }

      if (_disposed)
      {
        throw new ObjectDisposedException("DbContextCollection");
      }

      if (_completed)
      {
        throw new InvalidOperationException("You can't call Commit() or Rollback() more than once on a DbContextCollection. "
                                          + "All the changes in the DbContext instances managed by this collection have already been "
                                          + "saved or rollback and all database transactions have been completed and closed. If you wish "
                                          + "to make more data changes, create a new DbContextCollection and make your changes there.");
      }

      // See comments in the sync version of this method for more details.

      ExceptionDispatchInfo lastError = null;

      var c = 0;

      foreach (var dbContext in InitializedDbContexts.Values)
      {
        try
        {
          if (!_readOnly)
          {
            c += await dbContext.Proxy.SaveChangesDirectAsync(cancelToken).ConfigureAwait(false);
          }

          // If we've started an explicit database transaction, time to commit it now.
          var tran = getValueOrDefault(_transactions, dbContext.DbContext);
          if (tran != null)
          {
            tran.Commit();
            tran.Dispose();
          }
        }
        catch (Exception e)
        {
          lastError = ExceptionDispatchInfo.Capture(e);
        }
      }

      _transactions.Clear();
      _completed = true;

      if (lastError != null)
      {
        // Re-throw while maintaining the exception's original stack track
        lastError.Throw();
      }

      return c;
    }

    public void Rollback()
    {
      if (_disposed)
      {
        throw new ObjectDisposedException("DbContextCollection");
      }

      if (_completed)
      {
        throw new InvalidOperationException("You can't call Commit() or Rollback() more than once on a DbContextCollection. "
                                          + "All the changes in the DbContext instances managed by this collection have already been "
                                          + "saved or rollback and all database transactions have been completed and closed. If you "
                                          + "wish to make more data changes, create a new DbContextCollection and make your changes there.");
      }

      ExceptionDispatchInfo lastError = null;

      foreach (var dbContext in InitializedDbContexts.Values)
      {
        // There's no need to explicitly rollback changes in a DbContext as
        // DbContext doesn't save any changes until its SaveChanges() method is called.
        // So "rolling back" for a DbContext simply means not calling its SaveChanges()
        // method. 

        // But if we've started an explicit database transaction, then we must roll it back.
        var tran = getValueOrDefault(_transactions, dbContext.DbContext);
        if (tran != null)
        {
          try
          {
            tran.Rollback();
            tran.Dispose();
          }
          catch (Exception e)
          {
            lastError = ExceptionDispatchInfo.Capture(e);
          }
        }
      }

      _transactions.Clear();
      _completed = true;

      if (lastError != null)
      {
        // Re-throw while maintaining the exception's original stack track
        lastError.Throw();
      }
    }

    /// <summary>
    /// Returns the value associated with the specified key or the default
    /// value for the TValue  type.
    /// </summary>
    private static TValue getValueOrDefault<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
    {
      return dictionary.TryGetValue(key, out var value)
        ? value
        : default(TValue);
    }
  }
}
