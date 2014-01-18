// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.TestCommon
{
    /// <summary>
    /// This is an implementation of SynchronizationContext that not only queues things on the thread pool for
    /// later work, but also ensures that it sets itself back as the synchronization context (something that the
    /// default implementatation of SynchronizationContext does not do).
    /// </summary>
    public class ThreadPoolSyncContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object state)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                SynchronizationContext oldContext = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(this);
                d.Invoke(state);
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }, null);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            SynchronizationContext oldContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(this);
            d.Invoke(state);
            SynchronizationContext.SetSynchronizationContext(oldContext);
        }
    }
}
