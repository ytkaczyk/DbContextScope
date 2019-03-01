using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  internal class DbContextScopeFactory : IDbContextScopeFactory
  {
    private readonly IAmbientDbContextFactory _ambientDbContextFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IScopeDiagnostic _scopeDiagnostic;
    private readonly ILogger<DbContextScopeFactory> _logger;
    private bool _disposed;
    private List<WeakReference<IDisposable>> _disposables = new List<WeakReference<IDisposable>>();

    public DbContextScopeFactory(IAmbientDbContextFactory ambientDbContextFactory, ILoggerFactory loggerFactory, IScopeDiagnostic scopeDiagnostic = null)
    {
      _ambientDbContextFactory = ambientDbContextFactory;
      _loggerFactory = loggerFactory;
      _scopeDiagnostic = scopeDiagnostic;
      _logger = loggerFactory.CreateLogger<DbContextScopeFactory>();
    }

    public IDbContextScope Create(DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting)
    {
      checkDisposed();

      var scope = new DbContextScope(
        joiningOption,
        false,
        null,
        _ambientDbContextFactory,
        _loggerFactory,
        _scopeDiagnostic);

      _disposables.Add(new WeakReference<IDisposable>(scope));

      return scope;
    }

    public IDbContextReadOnlyScope CreateReadOnly(DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting)
    {
      checkDisposed();

      var scope = new DbContextReadOnlyScope(
        joiningOption,
        null,
        _ambientDbContextFactory,
        _loggerFactory,
        _scopeDiagnostic);

      _disposables.Add(new WeakReference<IDisposable>(scope));

      return scope;
    }

    public IDbContextScope CreateWithTransaction(IsolationLevel isolationLevel)
    {
      checkDisposed();

      var scope = new DbContextScope(
        DbContextScopeOption.ForceCreateNew,
        false,
        isolationLevel,
        _ambientDbContextFactory,
        _loggerFactory,
        _scopeDiagnostic);

      _disposables.Add(new WeakReference<IDisposable>(scope));

      return scope;
    }

    public IDbContextReadOnlyScope CreateReadOnlyWithTransaction(IsolationLevel isolationLevel)
    {
      checkDisposed();

      var scope = new DbContextReadOnlyScope(
        DbContextScopeOption.ForceCreateNew,
        isolationLevel,
        _ambientDbContextFactory,
        _loggerFactory,
        _scopeDiagnostic);

      _disposables.Add(new WeakReference<IDisposable>(scope));

      return scope;
    }

    public IDisposable SuppressAmbientContext()
    {
      checkDisposed();

      var suppressor = new AmbientContextSuppressor();

      _disposables.Add(new WeakReference<IDisposable>(suppressor));

      return suppressor;
    }

    private void checkDisposed()
    {
      if (_disposed)
      {
        throw new ObjectDisposedException(nameof(DbContextScopeFactory));
      }
    }

    public void Dispose()
    {
      if (!_disposed)
      {
        _disposed = true;

        foreach (var weakReference in Enumerable.Reverse(_disposables))
        {
          if (weakReference.TryGetTarget(out var disposable))
          {
            disposable.Dispose();
          }
        }

        _disposables = null;
      }
    }
  }
}
