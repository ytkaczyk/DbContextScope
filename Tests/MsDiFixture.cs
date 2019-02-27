using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbContextScope.Tests
{
  [TestClass]
  public class MsDiFixture
  {
    [TestMethod]
    public void Resolve_scoped_services_should_resolve_services_only_once()
    {
      // arrange
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddScoped<ServiceA>();
      serviceCollection.AddScoped<ServiceB>();
      var serviceProvider = serviceCollection.BuildServiceProvider();

      // act
      var serviceA = serviceProvider.GetRequiredService<ServiceA>();
      var serviceB = serviceProvider.GetRequiredService<ServiceB>();

      // assert
      Assert.AreEqual(serviceA.Id, serviceB.ServiceA.Id);
    }

    public class ServiceA
    {
      public string Id = Guid.NewGuid().ToString();
    }

    public class ServiceB
    {
      public ServiceA ServiceA { get; }

      public ServiceB(ServiceA serviceA)
      {
        ServiceA = serviceA;
      }
    }
  }
}