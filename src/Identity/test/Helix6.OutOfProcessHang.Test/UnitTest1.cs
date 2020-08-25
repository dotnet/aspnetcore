using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

            var sleepCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "timeout" : "sleep";

            await ProcessUtil.RunAsync(sleepCmd, "10", cancellationToken: new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        }
    }
}
