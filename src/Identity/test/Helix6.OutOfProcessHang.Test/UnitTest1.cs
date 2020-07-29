using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ProcessUtilities;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTestProject6
{
    public class UnitTest1
    {
        private ITestOutputHelper _testOutputHelper;

        public UnitTest1(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task OutOfProcessHang()
        {
            var path = typeof(ProcessWithHang.Program).Assembly.Location;

            _testOutputHelper.WriteLine($"About to execute: {path}");

            await ProcessUtil.RunAsync("dotnet", path, cancellationToken: new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        }
    }
}
