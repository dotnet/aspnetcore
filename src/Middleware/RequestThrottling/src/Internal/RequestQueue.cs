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
        private readonly SemaphoreSlim _serverSemaphore;
        //private readonly SemaphoreSlim _queueSemaphore;

        private object _waitingRequestsLock = new object();
        private int _waitingRequests;

        public RequestQueue(int maxConcurrentRequests, int requestQueueLimit)
        {
            _maxConcurrentRequests = maxConcurrentRequests;
            _requestQueueLimit = requestQueueLimit;
            //_queueSemaphore = new SemaphoreSlim(_requestQueueLimit);
            _serverSemaphore = new SemaphoreSlim(_maxConcurrentRequests);
        }

        // check total server capacity; only reject if it's all full

        public async Task<bool> TryEnterQueueAsync()
        {
            lock (_waitingRequestsLock)
            {
                if (_waitingRequests + ConcurrentRequests >= _requestQueueLimit + _maxConcurrentRequests)
                {
                    return false;
                }

                _waitingRequests++;
            }

            await _serverSemaphore.WaitAsync();

            lock (_waitingRequestsLock)
            {
                _waitingRequests--;
            }

            return true;
        }
        //{
        // a return value of 'false' indicates that the request is rejected
        // a return value of 'true' indicates that the request may proceed
        // _serverSemaphore.Release is *not* called in this method, it is called externally when requests leave the server

        // must check the queue before checking the concurrency limit, can't let requests skip the 

        //var enterQueueTask = _queueSemaphore.WaitAsync();
        //if (!enterQueueTask.IsCompletedSuccessfully)
        //{
        //    return false;
        //}

        //await _serverSemaphore.WaitAsync();
        //_queueSemaphore.Release();
        //return true;
        //}

        public bool TryEnterServerWithNoQueue()
        {
            return _serverSemaphore.WaitAsync().IsCompletedSuccessfully;
        }

        public void Release()
        {
            _serverSemaphore.Release();
        }

        public int ConcurrentRequests
        {
            get => _maxConcurrentRequests - _serverSemaphore.CurrentCount;
        }

        public int WaitingRequests
        {
            get => _waitingRequests; // _requestQueueLimit - _queueSemaphore.CurrentCount;
        }

        public void Dispose()
        {
            _serverSemaphore.Dispose();
        }
    }
}
