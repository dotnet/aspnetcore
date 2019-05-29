// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RequestThrottling.Internal
{
    internal class RequestQueue : IDisposable
    {
        private readonly int _maxConcurrentRequests;
        private readonly int _requestQueueLimit;
        private readonly SemaphoreSlim _semaphore;

        private object _waitingRequestsLock = new object();
        private int _waitingRequests;

        public RequestQueue(int maxConcurrentRequests, int requestQueueLimit)
        {
            _maxConcurrentRequests = maxConcurrentRequests;
            _requestQueueLimit = requestQueueLimit;
            _semaphore = new SemaphoreSlim(maxConcurrentRequests);
        }

        public async Task<bool> TryEnterQueueAsync()
        {
            lock (_waitingRequestsLock)
            {
                if (_waitingRequests >= _requestQueueLimit)
                {
                    return false;
                }

                _waitingRequests++;
            }

            await _semaphore.WaitAsync();

            lock (_waitingRequestsLock)
            {
                _waitingRequests--;
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
