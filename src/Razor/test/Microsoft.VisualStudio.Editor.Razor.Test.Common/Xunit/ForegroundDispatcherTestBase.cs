// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;

namespace Xunit
{
    public abstract class ForegroundDispatcherTestBase
    {
        internal ForegroundDispatcher Dispatcher { get; } = new SingleThreadedForegroundDispatcher();

        private class SingleThreadedForegroundDispatcher : ForegroundDispatcher
        {
            public SingleThreadedForegroundDispatcher()
            {
                ForegroundScheduler = SynchronizationContext.Current == null ? new ThrowingTaskScheduler() : TaskScheduler.FromCurrentSynchronizationContext();
                BackgroundScheduler = TaskScheduler.Default;
            }

            public override TaskScheduler ForegroundScheduler { get; }

            public override TaskScheduler BackgroundScheduler { get; }

            private Thread Thread { get; } = Thread.CurrentThread;

            public override bool IsForegroundThread => Thread.CurrentThread == Thread;
        }

        private class ThrowingTaskScheduler : TaskScheduler
        {
            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return Enumerable.Empty<Task>();
            }

            protected override void QueueTask(Task task)
            {
                throw new InvalidOperationException($"Use [{nameof(ForegroundFactAttribute)}]");
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                throw new InvalidOperationException($"Use [{nameof(ForegroundFactAttribute)}]");
            }
        }
    }
}
