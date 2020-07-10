using System;
using System.Threading;
using System.Threading.Tasks;
using ProcessUtilities;
using Xunit;

namespace XUnitTestProject3
{
    public class UnitTest1
    {
        static UnitTest1()
        {
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                if (e.Exception is TestTimeoutException)
                {
                    // Block forever, this allows the test host to collect a dump with the test still on the stack
                    new ManualResetEventSlim().Wait();
                }
            };
        }

        [Fact]
        public async Task AsyncTimeoutTest()
        {
            var tcs = new TaskCompletionSource<object>();

            await tcs.Task.OrTimeout();
        }
    }
}
