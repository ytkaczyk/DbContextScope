using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace EntityFrameworkCore.DbContextScope
{
  public static class AmbientContextScopeMagic
  {
    /*
       * This is where all the magic happens. And there is not much of it.
       * 
       * This implementation is inspired by the source code of the
       * TransactionScope class in .NET 4.5.1 (the TransactionScope class
       * is prior versions of the .NET Fx didn't have support for async
       * operations).
       * 
       * In order to understand this, you'll need to be familiar with the
       * concept of async points. You'll also need to be familiar with the
       * ExecutionContext and CallContext and understand how and why they 
       * flow through async points. Stephen Toub has written an
       * excellent blog post about this - it's a highly recommended read:
       * http://blogs.msdn.com/b/pfxteam/archive/2012/06/15/executioncontext-vs-synchronizationcontext.aspx
       * 
       * Overview: 
       * 
       * We want our DbContextScope instances to be ambient within 
       * the context of a logical flow of execution. This flow may be 
       * synchronous or it may be asynchronous.
       * 
       * If we only wanted to support the synchronous flow scenario, 
       * we could just store our DbContextScope instances in a ThreadStatic 
       * variable. That's the "traditional" (i.e. pre-async) way of implementing
       * an ambient context in .NET. You can see an example implementation of 
       * a TheadStatic-based ambient DbContext here: http://coding.abel.nu/2012/10/make-the-dbcontext-ambient-with-unitofworkscope/ 
       * 
       * But that would be hugely limiting as it would prevent us from being
       * able to use the new async features added to Entity Framework
       * in EF6 and .NET 4.5.
       * 
       * So we need a storage place for our DbContextScope instances 
       * that can flow through async points so that the ambient context is still 
       * available after an await (or any other async point). And this is exactly 
       * what CallContext is for.
       * 
       * There are however two issues with storing our DbContextScope instances 
       * in the CallContext:
       * 
       * 1) Items stored in the CallContext should be serializable. That's because
       * the CallContext flows not just through async points but also through app domain 
       * boundaries. I.e. if you make a remoting call into another app domain, the
       * CallContext will flow through this call (which will require all the values it
       * stores to get serialized) and get restored in the other app domain.
       * 
       * In our case, our DbContextScope instances aren't serializable. And in any case,
       * we most definitely don't want them to be flown accross app domains. So we'll
       * use the trick used by the TransactionScope class to work around this issue.
       * Instead of storing our DbContextScope instances themselves in the CallContext,
       * we'll just generate a unique key for each instance and only store that key in 
       * the CallContext. We'll then store the actual DbContextScope instances in a static
       * Dictionary against their key. 
       * 
       * That way, if an app domain boundary is crossed, the keys will be flown accross
       * but not the DbContextScope instances since a static variable is stored at the 
       * app domain level. The code executing in the other app domain won't see the ambient
       * DbContextScope created in the first app domain and will therefore be able to create
       * their own ambient DbContextScope if necessary.
       * 
       * 2) The CallContext is flow through *all* async points. This means that if someone
       * decides to create multiple threads within the scope of a DbContextScope, our ambient scope
       * will flow through all the threads. Which means that all the threads will see that single 
       * DbContextScope instance as being their ambient DbContext. So clients need to be 
       * careful to always suppress the ambient context before kicking off a parallel operation
       * to avoid our DbContext instances from being accessed from multiple threads.
       * 
       */

    private static readonly string ambientDbContextScopeKey = "AmbientDbcontext_" + Guid.NewGuid();

    // Use a ConditionalWeakTable instead of a simple ConcurrentDictionary to store our DbContextScope instances 
    // in order to prevent leaking DbContextScope instances if someone doesn't dispose them properly.
    //
    // For example, if we used a ConcurrentDictionary and someone let go of a DbContextScope instance without 
    // disposing it, our ConcurrentDictionary would still have a reference to it, preventing
    // the GC from being able to collect it => leak. With a ConditionalWeakTable, we don't hold a reference
    // to the DbContextScope instances we store in there, allowing them to get GCed.
    // The doc for ConditionalWeakTable isn't the best. This SO anser does a good job at explaining what 
    // it does: http://stackoverflow.com/a/18613811
    private static readonly ConditionalWeakTable<InstanceIdentifier, DbContextScope> dbContextScopeInstances = new ConditionalWeakTable<InstanceIdentifier, DbContextScope>();

    /// <summary>
    /// Makes the provided 'dbContextScope' available as the the ambient scope via the CallContext.
    /// </summary>
    internal static void SetAmbientScope(DbContextScope newAmbientScope)
    {
      if (newAmbientScope == null)
      {
        throw new ArgumentNullException(nameof(newAmbientScope));
      }

      //return Thread.CurrentThread.GetExecutionContextReader().LogicalCallContext.GetData(name);
      var current = CallContext.GetData(ambientDbContextScopeKey) as InstanceIdentifier;

      if (current == newAmbientScope.InstanceIdentifier)
      {
        return;
      }

      // Store the new scope's instance identifier in the CallContext, making it the ambient scope
      CallContext.SetData(ambientDbContextScopeKey, newAmbientScope.InstanceIdentifier);

      // Keep track of this instance (or do nothing if we're already tracking it)
      dbContextScopeInstances.GetValue(newAmbientScope.InstanceIdentifier, key => newAmbientScope);
    }

    /// <summary>
    /// Clears the ambient scope from the CallContext and stops tracking its instance.
    /// Call this when a DbContextScope is being disposed.
    /// </summary>
    internal static void RemoveAmbientScope()
    {
      var current = CallContext.GetData(ambientDbContextScopeKey) as InstanceIdentifier;
      CallContext.SetData(ambientDbContextScopeKey, null);

      // If there was an ambient scope, we can stop tracking it now
      if (current != null)
      {
        dbContextScopeInstances.Remove(current);
      }
    }

    /// <summary>
    /// Clears the ambient scope from the CallContext but keeps tracking its instance. Call this to temporarily
    /// hide the ambient context (e.g. to prevent it from being captured by parallel task).
    /// </summary>
    internal static void HideAmbientScope()
    {
      CallContext.SetData(ambientDbContextScopeKey, null);
    }

    /// <summary>
    /// Get the current ambient scope or null if no ambient scope has been setup.
    /// </summary>
    internal static DbContextScope GetAmbientScope()
    {
      // Retrieve the identifier of the ambient scope (if any)
      var instanceIdentifier = CallContext.GetData(ambientDbContextScopeKey) as InstanceIdentifier;
      if (instanceIdentifier == null)
      {
        return null; // Either no ambient context has been set or we've crossed an app domain boundary and have (intentionally) lost the ambient context
      }

      // Retrieve the DbContextScope instance corresponding to this identifier
      if (dbContextScopeInstances.TryGetValue(instanceIdentifier, out var ambientScope))
      {
        return ambientScope;
      }

      // We have an instance identifier in the CallContext but no corresponding instance
      // in our DbContextScopeInstances table. This should never happen! The only place where
      // we remove the instance from the DbContextScopeInstances table is in RemoveAmbientScope(),
      // which also removes the instance identifier from the CallContext. 
      //
      // There's only one scenario where this could happen: someone let go of a DbContextScope 
      // instance without disposing it. In that case, the CallContext
      // would still contain a reference to the scope and we'd still have that scope's instance
      // in our DbContextScopeInstances table. But since we use a ConditionalWeakTable to store 
      // our DbContextScope instances and are therefore only holding a weak reference to these instances, 
      // the GC would be able to collect it. Once collected by the GC, our ConditionalWeakTable will return
      // null when queried for that instance. In that case, we're OK. This is a programming error 
      // but our use of a ConditionalWeakTable prevented a leak.
      Debug.WriteLine("Programming error detected. Found a reference to an ambient DbContextScope in the "
                    + "CallContext but didn't have an instance for it in our DbContextScopeInstances table. "
                    + "This most likely means that this DbContextScope instance wasn't disposed of properly. "
                    + "DbContextScope instance must always be disposed. Review the code for any DbContextScope "
                    + "instance used outside of a 'using' block and fix it so that all DbContextScope instances are disposed of.");

      return null;
    }
  }
}
