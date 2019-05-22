// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RequestThrottling.Internal
{
    internal class SemaphoreWrapper : IDisposable
    {
        private SemaphoreSlim _semaphore;
        private int _waitingRequests;
        private object _waitingRequestsLock = new object();

        public SemaphoreWrapper(int queueLength)
        {
            _semaphore = new SemaphoreSlim(queueLength);
        }

        public async Task EnterQueue()
        {
            lock (_waitingRequestsLock)
            {
                _waitingRequests++;
            }

            await _semaphore.WaitAsync();

            lock (_waitingRequestsLock)
            {
                _waitingRequests--;
            }
        }

        public void Release()
        {
            _semaphore.Release();
        }

        public int Count
        {
            get => _semaphore.CurrentCount;
        }
        public int WaitingRequests
        {
            get => _waitingRequests;
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
