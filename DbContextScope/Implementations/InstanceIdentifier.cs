using System;

namespace EntityFrameworkCore.DbContextScope.Implementations
{
  /*
   * The idea of using an object reference as our instance identifier 
   * instead of simply using a unique string (which we could have generated
   * with Guid.NewGuid() for example) comes from the TransactionScope
   * class. As far as I can make out, a string would have worked just fine.
   * I'm guessing that this is done for optimization purposes. Creating
   * an empty class is cheaper and uses up less memory than generating
   * a unique string.
  */
  internal class InstanceIdentifier : MarshalByRefObject
  {
  }
}