using AspNetCoreSdkTests.Util;
using NUnit.Framework;

// Run all test cases in parallel
[assembly: Parallelizable(ParallelScope.Children)]

[SetUpFixture]
public class AssemblySetUp
{
    public static string TempDir { get; private set; }

    [OneTimeSetUp]
    public void SetUp()
    {
        TempDir = IOUtil.GetTempDir();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        IOUtil.DeleteDir(TempDir);
    }
}