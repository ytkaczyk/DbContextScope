using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.DbContextScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbContextScope.Tests
{
  [TestClass]
  public class DbContextScopeFactoryExtensionsFixture
  {
    [TestMethod]
    public void Create_should_create_a_proxy()
    {
      // arrange
      var counter = new CallCounter();

      // act
      var dummyDbContext = counter.Create<BlockingDummyDbContext>();

      // assert
      var dummyDbContextTypeName = dummyDbContext.GetType().Name;
      Assert.AreEqual("BlockingDummyDbContextProxy", dummyDbContextTypeName);
    }

    [TestMethod]
    public void Dispose_of_proxy_should_be_forwarded()
    {
      // arrange
      var counter = new CallCounter();
      var dummyDbContext = counter.Create<BlockingDummyDbContext>();

      // act
      dummyDbContext.Dispose();

      // assert
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "Dispose"), counter.ReportCalledMethods());
    }

    [TestMethod]
    public void SaveChanges_of_proxy_should_be_forwarded()
    {
      // arrange
      var counter = new CallCounter();
      var dummyDbContext = counter.Create<BlockingDummyDbContext>();

      // act
      var changes = dummyDbContext.SaveChanges();

      // assert
      Assert.AreEqual(1, changes);
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "SaveChanges"), counter.ReportCalledMethods());
    }

    [TestMethod]
    public void SaveChanges_with_bool_of_proxy_should_be_forwarded()
    {
      // arrange
      var counter = new CallCounter();
      var dummyDbContext = counter.Create<BlockingDummyDbContext>();

      // act
      var changes = dummyDbContext.SaveChanges(true);

      // assert
      Assert.AreEqual(1, changes);
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "SaveChanges"), counter.ReportCalledMethods());
    }

    [TestMethod]
    public async Task SaveChangesAsync_of_proxy_should_be_forwarded()
    {
      // arrange
      var counter = new CallCounter();
      var dummyDbContext = counter.Create<BlockingDummyDbContext>();

      // act
      var changes = await dummyDbContext.SaveChangesAsync();

      // assert
      Assert.AreEqual(1, changes);
      // note: since CancellationToken is a struct and the method from DbContext.SaveChangesAsync is defaulted with default(CancellationToken), the call always results in SaveChangesAsync(CancellationToken)
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "SaveChangesAsync(CancellationToken)"), counter.ReportCalledMethods());
    }

    [TestMethod]
    public async Task SaveChangesAsync_with_bool_of_proxy_should_be_forwarded()
    {
      // arrange
      var counter = new CallCounter();
      var dummyDbContext = counter.Create<BlockingDummyDbContext>();

      // act
      var changes = await dummyDbContext.SaveChangesAsync(true);

      // assert
      Assert.AreEqual(1, changes);
      // note: since CancellationToken is a struct and the method from DbContext.SaveChangesAsync is defaulted with default(CancellationToken), the call always results in SaveChangesAsync(CancellationToken)
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "SaveChangesAsync(CancellationToken)"), counter.ReportCalledMethods());
    }

    [TestMethod]
    public async Task SaveChangesAsync_with_cancellationToken_of_proxy_should_be_forwarded()
    {
      // arrange
      var counter = new CallCounter();
      var dummyDbContext = counter.Create<BlockingDummyDbContext>();

      // act
      var changes = await dummyDbContext.SaveChangesAsync(new CancellationToken(false));

      // assert
      Assert.AreEqual(1, changes);
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "SaveChangesAsync(CancellationToken)"), counter.ReportCalledMethods());
    }

    [TestMethod]
    public async Task SaveChangesAsync_with_bool_and_cancellationToken_of_proxy_should_be_forwarded()
    {
      // arrange
      var counter = new CallCounter();
      var dummyDbContext = counter.Create<BlockingDummyDbContext>();

      // act
      var changes = await dummyDbContext.SaveChangesAsync(true, new CancellationToken(false));

      // assert
      Assert.AreEqual(1, changes);
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "SaveChangesAsync(CancellationToken)"), counter.ReportCalledMethods());
    }

    [TestMethod]
    public void DummyContext_should_save_an_entity()
    {
      // arrange
      var dummyDbContext = new DummyDbContext();
      dummyDbContext.DummyEntities.Add(new DummyEntity());

      // act
      var changes = dummyDbContext.SaveChanges();

      // assert
      Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public void DummyContext_should_update_an_entity()
    {
      // arrange
      var dummyDbContext = new DummyDbContext();
      var dummyEntity = new DummyEntity{Name = "Bob"};
      dummyDbContext.DummyEntities.Add(dummyEntity);
      dummyDbContext.SaveChanges();

      dummyDbContext = new DummyDbContext();
      var savedDummyEntity = dummyDbContext.DummyEntities.Find(dummyEntity.Id);
      savedDummyEntity.Name = "Alice";

      // act
      var changes = dummyDbContext.SaveChanges();

      // assert
      Assert.AreEqual(1, changes);

      dummyDbContext = new DummyDbContext();
      savedDummyEntity = dummyDbContext.DummyEntities.Find(dummyEntity.Id);
      Assert.AreEqual("Alice", savedDummyEntity.Name);
    }

    [TestMethod]
    public void Add_entity_should_call_refesh_entities_in_parent_scope()
    {
      // arrange
      var counter = new CallCounter();
      var dummyDbContext = counter.Create<DummyDbContext>();
      dummyDbContext.DummyEntities.Add(new DummyEntity());

      // act
      var changes = dummyDbContext.SaveChanges();

      // assert
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "RefreshEntitiesInParentScope(IEnumerable)"), counter.ReportCalledMethods());
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "SaveChanges"), counter.ReportCalledMethods());
    }

    [TestMethod]
    public void Update_entity_should_call_refesh_entities_in_parent_scope()
    {
      // arrange
      var baseDummyDbContext = new DummyDbContext();
      var dummyEntity = new DummyEntity { Name = "Bob" };
      baseDummyDbContext.DummyEntities.Add(dummyEntity);
      baseDummyDbContext.SaveChanges();

      var counter = new CallCounter();
      var dummyDbContext = counter.Create<DummyDbContext>();
      dummyDbContext.ChangeTracker.Tracked += (sender, args) => Console.WriteLine("Tracked: " + args.Entry.GetType());
      dummyDbContext.ChangeTracker.StateChanged += (sender, args) => Console.WriteLine("StateChanged: " + args.Entry.GetType());
      var savedDummyEntity = dummyDbContext.DummyEntities.Find(dummyEntity.Id);
      savedDummyEntity.Name = "Alice";

      // act
      var changes = dummyDbContext.SaveChanges();

      // assert
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "RefreshEntitiesInParentScope(IEnumerable)"), counter.ReportCalledMethods());
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "SaveChanges"), counter.ReportCalledMethods());
    }

    [TestMethod]
    public async Task Add_entity_should_call_refesh_entities_async_in_parent_scope()
    {
      // arrange
      var counter = new CallCounter();
      var dummyDbContext = counter.Create<DummyDbContext>();
      dummyDbContext.Add(new DummyEntity());

      // act
      var changes = await dummyDbContext.SaveChangesAsync();

      // assert
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "RefreshEntitiesInParentScopeAsync(IEnumerable)"), counter.ReportCalledMethods());
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "SaveChangesAsync(CancellationToken)"), counter.ReportCalledMethods());
    }

    [TestMethod]
    public async Task Update_entity_should_call_refesh_entities_async_in_parent_scope()
    {
      // arrange
      var baseDummyDbContext = new DummyDbContext();
      var dummyEntity = new DummyEntity { Name = "Bob" };
      await baseDummyDbContext.DummyEntities.AddAsync(dummyEntity);
      await baseDummyDbContext.SaveChangesAsync();

      var counter = new CallCounter();
      var dummyDbContext = counter.Create<DummyDbContext>();
      dummyDbContext.ChangeTracker.Tracked += (sender, args) => Console.WriteLine("Tracked: " + args.Entry.GetType());
      dummyDbContext.ChangeTracker.StateChanged += (sender, args) => Console.WriteLine("StateChanged: " + args.Entry.GetType());
      var savedDummyEntity = await dummyDbContext.DummyEntities.FindAsync(dummyEntity.Id);
      savedDummyEntity.Name = "Alice";

      // act
      var changes = await dummyDbContext.SaveChangesAsync();

      // assert
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "RefreshEntitiesInParentScopeAsync(IEnumerable)"), counter.ReportCalledMethods());
      Assert.AreEqual(1, counter.CalledMethods.Count(cm => cm == "SaveChangesAsync(CancellationToken)"), counter.ReportCalledMethods());
    }

    private class CallCounter : IDbContextScopeFactory, IDbContextScope
    {
      public readonly List<string> CalledMethods = new List<string>();

      public string ReportCalledMethods()
      {
        return "Called Method(s): " + string.Join(", ", CalledMethods);
      }

      IDbContextScope IDbContextScopeFactory.Create(DbContextScopeOption joiningOption)
      {
        return this;
      }

      IDbContextReadOnlyScope IDbContextScopeFactory.CreateReadOnly(DbContextScopeOption joiningOption)
      {
        return this;
      }

      IDbContextScope IDbContextScopeFactory.CreateWithTransaction(IsolationLevel isolationLevel)
      {
        return this;
      }

      IDbContextReadOnlyScope IDbContextScopeFactory.CreateReadOnlyWithTransaction(IsolationLevel isolationLevel)
      {
        return this;
      }

      IDisposable IDbContextScopeFactory.SuppressAmbientContext()
      {
        return this;
      }

      void IDisposable.Dispose()
      {
        CalledMethods.Add("Dispose");
      }

      int IDbContextScope.SaveChanges()
      {
        CalledMethods.Add("SaveChanges");

        return 1;
      }

      Task<int> IDbContextScope.SaveChangesAsync()
      {
        CalledMethods.Add("SaveChangesAsync");

        return Task.FromResult(1);
      }

      Task<int> IDbContextScope.SaveChangesAsync(CancellationToken cancelToken)
      {
        CalledMethods.Add("SaveChangesAsync(CancellationToken)");

        return Task.FromResult(1);
      }

      void IDbContextScope.RefreshEntitiesInParentScope(IEnumerable entities)
      {
        CalledMethods.Add("RefreshEntitiesInParentScope(IEnumerable)");
      }

      Task IDbContextScope.RefreshEntitiesInParentScopeAsync(IEnumerable entities, CancellationToken cancellationToken)
      {
        CalledMethods.Add("RefreshEntitiesInParentScopeAsync(IEnumerable)");

        return Task.CompletedTask;
      }

      TDbContext IDbContextReadOnlyScope.Get<TDbContext>()
      {
        if (typeof(TDbContext) == typeof(BlockingDummyDbContext))
        {
          return (TDbContext)(object)new BlockingDummyDbContext();
        }

        if (typeof(TDbContext) == typeof(DummyDbContext))
        {
          return (TDbContext)(object)new DummyDbContext();
        }

        throw new NotSupportedException($"DbContext of type '{typeof(TDbContext)}' was unexpected at this point.");
      }
    }

    public class DummyDbContext : DbContext
    {
      public DummyDbContext()
      {
        
      }

      public static InMemoryDatabaseRoot GlobalDbRoot = new InMemoryDatabaseRoot();

      public DbSet<DummyEntity> DummyEntities { get; set; }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.UseInMemoryDatabase("STATIC_DummyDbContext", GlobalDbRoot);
        //optionsBuilder.UseInMemoryDatabase($"DummyDbContext_{Guid.NewGuid()}");
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DummyEntity>();
      }
    }

    public class DummyEntity
    {
      public int Id { get; set; }
      public string Name { get; set; }
    }

    public class BlockingDummyDbContext : DummyDbContext
    {
      public override int SaveChanges()
      {
        throw new AssertFailedException("SaveChanges() is not allowed to be called yet a proxy should intercept it.");
      }

      public override int SaveChanges(bool acceptAllChangesOnSuccess)
      {
        throw new AssertFailedException("SaveChanges(bool acceptAllChangesOnSuccess) is not allowed to be called yet a proxy should intercept it.");
      }

      public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
      {
        throw new AssertFailedException("SaveChangesAsync(CancellationToken cancellationToken) is not allowed to be called yet a proxy should intercept it.");
      }

      public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
      {
        throw new AssertFailedException("SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken) is not allowed to be called yet a proxy should intercept it.");
      }

      public override void Dispose()
      {
        throw new AssertFailedException("Dispose() is not allowed to be called yet a proxy should intercept it.");
      }
    }
  }
}
