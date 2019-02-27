using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.DbContextScope;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbContextScope.Tests
{
  [TestClass]
  public class CallContextFixture
  {
    [TestMethod]
    public void WhenFlowingData_ThenCanUseContext()
    {
      var d1 = new object();
      var t1 = default(object);
      var t10 = default(object);
      var t11 = default(object);
      var t12 = default(object);
      var t13 = default(object);
      var d2 = new object();
      var t2 = default(object);
      var t20 = default(object);
      var t21 = default(object);
      var t22 = default(object);
      var t23 = default(object);

      Task.WaitAll(
          Task.Run(() =>
          {
            CallContext.SetData("d1", d1);
            new Thread(() => t10 = CallContext.GetData<object>("d1")).Start();
            Task.WaitAll(
                      Task.Run(() => t1 = CallContext.GetData<object>("d1"))
                          .ContinueWith(t => Task.Run(() => t11 = CallContext.GetData<object>("d1"))),
                      Task.Run(() => t12 = CallContext.GetData<object>("d1")),
                      Task.Run(() => t13 = CallContext.GetData<object>("d1"))
                  );
          }),
          Task.Run(() =>
          {
            CallContext.SetData("d2", d2);
            new Thread(() => t20 = CallContext.GetData<object>("d2")).Start();
            Task.WaitAll(
                      Task.Run(() => t2 = CallContext.GetData<object>("d2"))
                          .ContinueWith(t => Task.Run(() => t21 = CallContext.GetData<object>("d2"))),
                      Task.Run(() => t22 = CallContext.GetData<object>("d2")),
                      Task.Run(() => t23 = CallContext.GetData<object>("d2"))
                  );
          })
      );

      Assert.AreSame(d1, t1);
      Assert.AreSame(d1, t10);
      Assert.AreSame(d1, t11);
      Assert.AreSame(d1, t12);
      Assert.AreSame(d1, t13);

      Assert.AreSame(d2, t2);
      Assert.AreSame(d2, t20);
      Assert.AreSame(d2, t21);
      Assert.AreSame(d2, t22);
      Assert.AreSame(d2, t23);

      Assert.IsNull(CallContext.GetData<object>("d1"));
      Assert.IsNull(CallContext.GetData<object>("d2"));
    }

    [TestMethod]
    public void SettingValueOnOneThreadDoesNotAffectTheOther()
    {
      var mre1 = new ManualResetEvent(false);
      var mre2 = new ManualResetEvent(false);
      int result = 0;

      var t1 = new Thread((_) =>
      {
        CallContext.SetData("foo", 1);
        mre1.Set();
        mre2.WaitOne();
        result = CallContext.GetData<int>("foo");
      });

      var t2 = new Thread((_) =>
      {
        mre1.WaitOne();
        CallContext.SetData("foo", 2);
        mre2.Set();
      });

      t1.Start();
      t2.Start();

      t1.Join();
      t2.Join();

      Assert.AreEqual(1, result);
    }
  }
}
