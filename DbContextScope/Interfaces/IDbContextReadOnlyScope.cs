using System;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.DbContextScope
{
  /// <summary>
  /// A read-only DbContextScope. Refer to the comments for IDbContextScope
  /// for more details.
  /// </summary>
  public interface IDbContextReadOnlyScope : IDisposable
  {
    TDbContext Get<TDbContext>() where TDbContext : DbContext;
  }
}
