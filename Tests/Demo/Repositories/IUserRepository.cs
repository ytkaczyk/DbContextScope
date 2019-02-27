using System;
using System.Threading.Tasks;
using DbContextScope.Tests.Demo.DomainModel;

namespace DbContextScope.Tests.Demo.Repositories
{
  public interface IUserRepository
  {
    User Get(Guid userId);

    Task<User> GetAsync(Guid userId);

    void Add(User user);
  }
}
