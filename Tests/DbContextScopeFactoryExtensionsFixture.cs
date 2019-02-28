﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.DbContextScope;
using Microsoft.EntityFrameworkCore;
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
      var dummyDbContext = counter.Create<DummyDbContext>();

      // assert
      var dummyDbContextTypeName = dummyDbContext.GetType().Name;
      Assert.AreEqual("DummyDbContextProxy", dummyDbContextTypeName);
    }

    [TestMethod]
    public void Dispose_of_proxy_should_be_forwarded()
    {
      // arrange
      var counter = new CallCounter();
      var dummyDbContext = counter.Create<DummyDbContext>();

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
      var dummyDbContext = counter.Create<DummyDbContext>();

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
      var dummyDbContext = counter.Create<DummyDbContext>();

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
      var dummyDbContext = counter.Create<DummyDbContext>();

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
      var dummyDbContext = counter.Create<DummyDbContext>();

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
      var dummyDbContext = counter.Create<DummyDbContext>();

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
      var dummyDbContext = counter.Create<DummyDbContext>();

      // act
      var changes = await dummyDbContext.SaveChangesAsync(true, new CancellationToken(false));

      // assert
      Assert.AreEqual(1, changes);
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

      Task IDbContextScope.RefreshEntitiesInParentScopeAsync(IEnumerable entities)
      {
        CalledMethods.Add("RefreshEntitiesInParentScopeAsync(IEnumerable)");
        
        return Task.CompletedTask;
      }

      TDbContext IDbContextReadOnlyScope.Get<TDbContext>()
      {
        return (TDbContext)(object)new DummyDbContext();
      }
    }

    public class DummyDbContext : DbContext
    {
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DummyEntity>();
      }

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
    public class DummyEntity
    {
      public int Id { get; set; }
    }
  }
}
