using System.Collections.Generic;

namespace EntityFrameworkCore.DbContextScope
{
  public interface IScopeDiagnostic
  {
    List<string> CalledMethods { get; }
  }
}
