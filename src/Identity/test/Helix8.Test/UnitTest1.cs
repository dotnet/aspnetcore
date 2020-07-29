using System;
using System.Threading;
using System.Threading.Tasks;
using ProcessUtilities;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTestProject8
{
    public class UnitTest1
    {
        private ITestOutputHelper _testOutputHelper;

        public UnitTest1(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task OutOfProcessCrash()
        {
            var path = typeof(ProcessWithCrash.Program).Assembly.Location;

            _testOutputHelper.WriteLine($"About to execute: {path}");

            await ProcessUtil.RunAsync("dotnet", path);
        }
    }
}
