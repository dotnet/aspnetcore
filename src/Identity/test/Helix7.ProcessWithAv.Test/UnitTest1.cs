using System;
using System.Threading;
using System.Threading.Tasks;
using ProcessUtilities;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTestProject7
{
    public class UnitTest1
    {
        private ITestOutputHelper _testOutputHelper;

        public UnitTest1(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task OutOfProcessAv()
        {
            var path = typeof(ProcessWithAv.Program).Assembly.Location;

            _testOutputHelper.WriteLine($"About to execute: {path}");

            await ProcessUtil.RunAsync("dotnet", path);
        }
    }
}
