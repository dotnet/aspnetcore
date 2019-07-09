using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.RequestThrottling.Tests.PolicyTests
{
    public static class StackQueueTests
    {
        [Fact]
        public static void BaseFunctionality()
        {
            var stack = new StackQueuePolicy(Options.Create(new QueuePolicyOptions {
                MaxConcurrentRequests = 0,
                RequestQueueLimit = 2,
            }));

            var task1 = stack.TryEnterAsync();

            Assert.False(task1.IsCompleted);

            stack.OnExit();

            Assert.True(task1.IsCompleted && task1.Result);
        }

        [Fact]
        public static void OldestRequestOverwritten()
        {
            var stack = new StackQueuePolicy(Options.Create(new QueuePolicyOptions {
                MaxConcurrentRequests = 0,
                RequestQueueLimit = 3,
            }));

            var task1 = stack.TryEnterAsync();
            Assert.False(task1.IsCompleted);
            var task2 = stack.TryEnterAsync();
            Assert.False(task2.IsCompleted);
            var task3 = stack.TryEnterAsync();
            Assert.False(task3.IsCompleted);

            var task4 = stack.TryEnterAsync();
            Assert.False(task4.IsCompleted);

            Assert.True(task1.IsCompleted);
            Assert.False(task1.Result);
        }

        [Fact]
        public static void RespectsMaxConcurrency()
        {
            var stack = new StackQueuePolicy(Options.Create(new QueuePolicyOptions {
                MaxConcurrentRequests = 2,
                RequestQueueLimit = 2,
            }));

            var task1 = stack.TryEnterAsync();
            Assert.True(task1.IsCompleted);

            var task2 = stack.TryEnterAsync();
            Assert.True(task2.IsCompleted);

            var task3 = stack.TryEnterAsync();
            Assert.False(task3.IsCompleted);
        }

        [Fact]
        public static void ExitRequestsPreserveSemaphoreState()
        {
            var stack = new StackQueuePolicy(Options.Create(new QueuePolicyOptions {
                MaxConcurrentRequests = 1,
                RequestQueueLimit = 2,
            }));

            var task1 = stack.TryEnterAsync();
            Assert.True(task1.IsCompleted && task1.Result);

            var task2 = stack.TryEnterAsync();
            Assert.False(task2.IsCompleted);

            stack.OnExit();  // t1 exits, should free t2 to return
            Assert.True(task2.IsCompleted && task2.Result);

            stack.OnExit();  // t2 exists, there's now a free spot in server

            var task3 = stack.TryEnterAsync();
            Assert.True(task3.IsCompleted && task3.Result);
        }

        [Fact]
        public static void StaleRequestsAreProperlyOverwritten()
        {
            var stack = new StackQueuePolicy(Options.Create(new QueuePolicyOptions
            {
                MaxConcurrentRequests = 0,
                RequestQueueLimit = 4,
            }));

            var task1 = stack.TryEnterAsync();
            stack.OnExit();
            Assert.True(task1.IsCompleted);

            var task2 = stack.TryEnterAsync();
            stack.OnExit();
            Assert.True(task2.IsCompleted);
        }

        [Fact]
        public static async Task OneTryEnterAsyncOneOnExit()
        {
            var stack = new StackQueuePolicy(Options.Create(new QueuePolicyOptions
            {
                MaxConcurrentRequests = 1,
                RequestQueueLimit = 4,
            }));

            Assert.Throws<InvalidOperationException>(() => stack.OnExit());

            await stack.TryEnterAsync();

            stack.OnExit();

            Assert.Throws<InvalidOperationException>(() => stack.OnExit());
        }
    } 
}
