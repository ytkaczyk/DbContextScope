using KellermanSoftware.CompareNetObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace DbContextScope.Tests
{
    public static class DeepComparison
    {
        public static ComparisonResult Compare<U, V>(U u, V v) 
            where U : class
            where V : class
        {
            CompareLogic compareLogic = new CompareLogic(
                new ComparisonConfig
                {
                    IgnoreObjectTypes = true
                });
            return compareLogic.Compare(u, v);
        }

        public static void ValidateCompare<U, V>(U u, V v)
            where U : class
            where V : class
        {
            var result = DeepComparison.Compare(u, v);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
        }
    }
}
