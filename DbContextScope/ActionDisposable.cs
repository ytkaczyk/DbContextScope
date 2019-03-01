using System;

namespace EntityFrameworkCore.DbContextScope
{
  internal class ActionDisposable : IDisposable
  {
    private Action _onDisposeAction;
    private readonly object _lockObject = new object();

    public ActionDisposable(Action onDisposeAction)
    {
      this._onDisposeAction = onDisposeAction;
    }

    public void Dispose()
    {
      lock (_lockObject)
      {
        _onDisposeAction?.Invoke();
        _onDisposeAction = null;
      }
    }
  }
}