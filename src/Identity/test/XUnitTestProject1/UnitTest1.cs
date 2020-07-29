using System;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTestProject1
{
    public class UnitTest1
    {
        [Fact]
        public async Task HangTestRunner()
        {
            var tcs = new TaskCompletionSource<object>();

            await tcs.Task;
        }

        [Fact]
        public async Task SlowTest()
        {
            await Task.Delay(1000);
        }

        [Fact]
        public async Task SlowerTest()
        {
            await Task.Delay(5000);
        }
    }
}
