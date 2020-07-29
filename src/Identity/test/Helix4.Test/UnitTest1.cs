using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTestProject4
{
    public class OtherComponent
    {
        public static void Execute(TaskCompletionSource<object> tcs)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                BuggyCode();

                tcs.TrySetResult(null);
            },
            null);

            static void BuggyCode() => throw new InvalidOperationException();
        }
    }

    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            await Task.Delay(1000);
        }

        [Fact]
        public async Task CrashTestRunner()
        {
            var tcs = new TaskCompletionSource<object>();

            OtherComponent.Execute(tcs);

            await tcs.Task;
        }


        [Fact]
        public async Task Test2()
        {
            await Task.Delay(5000);
            Assert.True(false);
        }
    }
}
