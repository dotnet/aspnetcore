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
        private int _waitingRequests;

        private readonly int _maxConcurrentRequests;
        private readonly int _requestQueueLimit;

        public RequestQueue(int maxConcurrentRequests, int requestQueueLimit)
        {
            _maxConcurrentRequests = maxConcurrentRequests;
            _requestQueueLimit = requestQueueLimit;
            _semaphore = new SemaphoreSlim(maxConcurrentRequests);
        }

        public RequestQueue(int maxConcurrentRequests) : this(maxConcurrentRequests, 5000) { }

        public async Task<bool> TryEnterQueueAsync()
        {
            // first, check the queue limits
            if (WaitingRequests > _requestQueueLimit)
            {
                return false;
            }

            var waitInQueueTask = _semaphore.WaitAsync();
            if (!waitInQueueTask.IsCompletedSuccessfully)
            {
                Interlocked.Increment(ref _waitingRequests);
                await waitInQueueTask;
                Interlocked.Decrement(ref _waitingRequests);
            }

            return true;
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
            get => _maxConcurrentRequests - _semaphore.CurrentCount;
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
