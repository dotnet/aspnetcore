using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Tests
{
    public class QueuePolicyAttributeTests
    {
        [Fact]
        public void DoesNotWaitIfSpaceAvailible()
        {
            var s = TestUtils.CreateQueuePolicyAttribute(2);

            var t1 = s.TryEnterAsync();
            Assert.True(t1.IsCompleted);

            var t2 = s.TryEnterAsync();
            Assert.True(t2.IsCompleted);

            var t3 = s.TryEnterAsync();
            Assert.False(t3.IsCompleted);
        }

        [Fact]
        public async Task WaitsIfNoSpaceAvailible()
        {
            var s = TestUtils.CreateQueuePolicyAttribute(1);
            Assert.True(await s.TryEnterAsync().OrTimeout());

            var waitingTask = s.TryEnterAsync();
            Assert.False(waitingTask.IsCompleted);

            s.OnExit();
            Assert.True(await waitingTask.OrTimeout());
        }

        [Fact]
        public async Task IsEncapsulated()
        {
            var s1 = TestUtils.CreateQueuePolicyAttribute(1);
            var s2 = TestUtils.CreateQueuePolicy(1);

            Assert.True(await s1.TryEnterAsync().OrTimeout());
            Assert.True(await s2.TryEnterAsync().OrTimeout());
        }
    }
}
