using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Tests.PolicyTests
{
    public static class ResettableBooleanCompletionSourceTests
     {
        private static StackPolicy _testQueue = TestUtils.CreateStackPolicy(8);

        [Fact]
        public async static void CanBeAwaitedMultipleTimes()
        {
            var tcs = new ResettableBooleanCompletionSource(_testQueue);

            tcs.Complete(true);
            Assert.True(await tcs.GetValueTask());

            tcs.Complete(true);
            Assert.True(await tcs.GetValueTask());

            tcs.Complete(false);
            Assert.False(await tcs.GetValueTask());

            tcs.Complete(false);
            Assert.False(await tcs.GetValueTask());
        }

        [Fact]
        public async static void CanSetResultToTrue()
        {
            var tcs = new ResettableBooleanCompletionSource(_testQueue);

            _ = Task.Run(() =>
            {
                tcs.Complete(true);
            });

            var result = await tcs.GetValueTask();
            Assert.True(result);
        }

        [Fact]
        public async static void CanSetResultToFalse()
        {
            var tcs = new ResettableBooleanCompletionSource(_testQueue);

            _ = Task.Run(() =>
            {
                tcs.Complete(false);
            });

            var result = await tcs.GetValueTask();
            Assert.False(result);
        }

        [Fact]
        public static void DoubleCallToGetResultCausesError()
        {
            // important to verify it throws rather than acting like a new task

            var tcs = new ResettableBooleanCompletionSource(_testQueue);
            var task = tcs.GetValueTask();
            tcs.Complete(true);

            Assert.True(task.Result);
            Assert.Throws<InvalidOperationException>(() => task.Result);
        }
    }
}
