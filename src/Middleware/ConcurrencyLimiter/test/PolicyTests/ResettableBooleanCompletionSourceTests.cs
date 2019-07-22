using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Tests.PolicyTests
{
     public static class ResettableBooleanCompletionSourceTests
     {
        private static LIFOQueuePolicy _testQueue = TestUtils.CreateLIFOPolicy(8);

        [Fact]
        public async static void CanBeAwaitedMultipleTimes()
        {
            var tcs = new ResettableBooleanCompletionSource(_testQueue);

            tcs.Complete(true);
            Assert.True(await tcs.Task());

            tcs.Complete(true);
            Assert.True(await tcs.Task());

            tcs.Complete(false);
            Assert.False(await tcs.Task());

            tcs.Complete(false);
            Assert.False(await tcs.Task());
        }

        [Fact]
        public async static void CanSetResultToTrue()
        {
            var tcs = new ResettableBooleanCompletionSource(_testQueue);

            _ = Task.Run(() =>
            {
                tcs.Complete(true);
            });

            var result = await tcs.Task();
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

            var result = await tcs.Task();
            Assert.False(result);
        }

        [Fact]
        public static void DoubleCallToGetResultCausesError()
        {
            // this isn't necesarrily desireable behavior, but upstream stuff takes it into account
            // so it's important to verify?

            var tcs = new ResettableBooleanCompletionSource(_testQueue);
            var task = tcs.Task();
            tcs.Complete(true);

            Assert.True(task.Result);
            Assert.Throws<InvalidOperationException>(() => task.Result);
        }
    }
}
