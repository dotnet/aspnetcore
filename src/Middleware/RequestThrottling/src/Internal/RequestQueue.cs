// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RequestThrottling.Internal
{
    internal class RequestQueue : IDisposable
    {
        private SemaphoreSlim _semaphore;
        private object _waitingRequestsLock = new object();

        public readonly int MaxConcurrentRequests;
        public readonly int RequestQueueLimit;
        public int WaitingRequests { get; private set; }

        public RequestQueue(int maxConcurrentRequests, int requestQueueLimit)
        {
            MaxConcurrentRequests = maxConcurrentRequests;
            RequestQueueLimit = requestQueueLimit;
            _semaphore = new SemaphoreSlim(maxConcurrentRequests);
        }

        public async Task EnterQueue()
        {
            var waitInQueueTask = _semaphore.WaitAsync();

            var needsToWaitOnQueue = !waitInQueueTask.IsCompletedSuccessfully;
            if (needsToWaitOnQueue)
            {
                lock (_waitingRequestsLock)
                {
                    WaitingRequests++;
                }

                await waitInQueueTask;

                lock (_waitingRequestsLock)
                {
                    WaitingRequests--;
                }
            }
        }

        public bool QueueLengthExceeded
        {
            get
            {
                lock (_waitingRequestsLock)
                {
                    return WaitingRequests > RequestQueueLimit;
                }
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

        public int ConcurrentRequests
        {
            get => MaxConcurrentRequests - _semaphore.CurrentCount;
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
