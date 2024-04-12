// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal abstract class StampedeState
    {
        protected StampedeState(in StampedeKey key) => Key = key;

        public StampedeKey Key { get; }

        public override string ToString() => Key.ToString();

        // because multiple callers can enlist, we need to track when the *last* caller cancels
        // (and keep going until then); that means we need to run with custom cancellation
        private readonly CancellationTokenSource sharedCancellation = new();

        protected abstract void SetCanceled();

        public CancellationToken SharedToken => sharedCancellation.Token;

        protected object SyncLock => this; // not exposed externally; we'll use the instance as the lock

        public int DebugCallerCount => Volatile.Read(ref activeCallers);

        private int activeCallers = 1;
        public void RemoveCaller()
        {
            lock (SyncLock)
            {
                if (--activeCallers == 0)
                {
                    // nobody is left, we're done
                    sharedCancellation.Cancel();
                    SetCanceled();
                }
            }
        }

        public bool TryAddCaller()
        {
            lock (SyncLock)
            {
                if (activeCallers <= 0)
                {
                    return false; // already burned
                }
                activeCallers++;
            }
            return true;
        }
    }
}
