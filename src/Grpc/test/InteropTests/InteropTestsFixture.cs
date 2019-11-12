using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace InteropTests
{
    public class InteropTestsFixture : IDisposable
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
        private WebServerProcess _process;

        public async Task EnsureStarted(ITestOutputHelper output)
        {
            if (_process != null)
            {
                return;
            }

            var webPath = @"C:\Development\Source\AspNetCore\src\Grpc\test\testassets\InteropTestsWebsite\";

            _process = new WebServerProcess(webPath, output);

            await _process.WaitForReady().TimeoutAfter(DefaultTimeout);
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
