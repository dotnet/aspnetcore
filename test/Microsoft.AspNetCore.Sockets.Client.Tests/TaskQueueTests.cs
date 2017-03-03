// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets.Client.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Client.Tests
{
    public class TaskQueueTests
    {
        [Fact]
        public async Task DrainingTaskQueueShutsQueueOff()
        {
            var queue = new TaskQueue();
            await queue.Enqueue(() => Task.CompletedTask);
            await queue.Drain();

            // This would throw if the task was queued successfully
            await queue.Enqueue(() => Task.FromException(new Exception()));
        }

        [Fact]
        public async Task TaskQueueDoesNotQueueNewTasksIfPreviousTaskFaulted()
        {
            var exception = new Exception();
            var queue = new TaskQueue();
            var ignore = queue.Enqueue(() => Task.FromException(exception));
            var task = queue.Enqueue(() => Task.CompletedTask);

            var actual = await Assert.ThrowsAsync<Exception>(async () => await task);
            Assert.Same(exception, actual);
        }

        [Fact]
        public void TaskQueueRunsTasksInSequence()
        {
            var queue = new TaskQueue();
            int n = 0;
            queue.Enqueue(() =>
            {
                n = 1;
                return Task.CompletedTask;
            });

            Task task = queue.Enqueue(() =>
            {
                return Task.Delay(100).ContinueWith(t => n = 2);
            });

            task.Wait();
            Assert.Equal(n, 2);
        }
    }
}
