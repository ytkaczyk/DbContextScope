using System;
using System.Threading.Tasks;
using DbContextScopeTests.Demo.DomainModel;

namespace DbContextScopeTests.Demo.Repositories
{
  public interface IUserRepository
  {
    User Get(Guid userId);

    Task<User> GetAsync(Guid userId);

    void Add(User user);
  }
}
