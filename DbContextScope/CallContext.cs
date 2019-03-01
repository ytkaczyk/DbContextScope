using System;
using System.Collections.Concurrent;
using System.Threading;

namespace EntityFrameworkCore.DbContextScope
{
  /// <summary>
  /// Provides a way to set contextual data that flows with the call and async context of a test or invocation.
  /// </summary>
  /// <remarks>
  /// Represents the CallContext form .netframework for .netcore.
  /// https://www.cazzulino.com/callcontext-netstandard-netcore.html
  /// </remarks>
  public static class CallContext
  {
    /// <summary>
    /// Stores a given object and associates it with the specified name.
    /// </summary>
    /// <param name="name">The name with which to associate the new item in the call context.</param>
    /// <param name="data">The object to store in the call context.</param>
    public static void SetData<T>(string name, T data) =>
      TypedCallContext<T>.State.GetOrAdd(name, _ => new AsyncLocal<T>()).Value = data;

    /// <summary>
    /// Retrieves an object with the specified name from the <see cref="CallContext"/>.
    /// </summary>
    /// <typeparam name="T">The type of the data being retrieved. Must match the type used when the <paramref name="name"/> was set via <see cref="SetData{T}(string, T)"/>.</typeparam>
    /// <param name="name">The name of the item in the call context.</param>
    /// <returns>The object in the call context associated with the specified name, or a default value for <typeparamref name="T"/> if none is found.</returns>
    public static T GetData<T>(string name) =>
      TypedCallContext<T>.State.TryGetValue(name, out AsyncLocal<T> data) ? data.Value : default(T);

    public static IDisposable OpenScope(string name)
    {
      SetData(name, new object());
      return new ActionDisposable(() => SetData<object>(name, null));
    }

    public static bool IsInScope(string name)
    {
      return GetData<object>(name) != null;
    }

    private static class TypedCallContext<T>
    {
      public static readonly ConcurrentDictionary<string, AsyncLocal<T>> State = new ConcurrentDictionary<string, AsyncLocal<T>>();
    }
  }
}
