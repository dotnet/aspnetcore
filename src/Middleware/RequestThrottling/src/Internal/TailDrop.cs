// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RequestThrottling.Internal
{
    internal class TailDrop : IRequestQueue
    {
        private readonly int _maxConcurrentRequests;
        private readonly int _requestQueueLimit;
        private readonly SemaphoreSlim _serverSemaphore;

        private object _totalRequestsLock = new object();
        public int TotalRequests { get; private set; }

        public TailDrop(int maxConcurrentRequests, int requestQueueLimit)
        {
            _maxConcurrentRequests = maxConcurrentRequests;
            _requestQueueLimit = requestQueueLimit;
            _serverSemaphore = new SemaphoreSlim(_maxConcurrentRequests);
        }

        public async Task<bool> TryEnterQueueAsync()
        {
            // a return value of 'false' indicates that the request is rejected
            // a return value of 'true' indicates that the request may proceed
            // _serverSemaphore.Release is *not* called in this method, it is called externally when requests leave the server

            lock (_totalRequestsLock)
            {
                if (TotalRequests >= _requestQueueLimit + _maxConcurrentRequests)
                {
                    return false;
                }

                TotalRequests++;
            }

            await _serverSemaphore.WaitAsync();

            return true;
        }

        public void Release()
        {
            _serverSemaphore.Release();

            lock (_totalRequestsLock)
            {
                TotalRequests--;
            }
        }

        public void Dispose()
        {
            _serverSemaphore.Dispose();
        }
    }
}
