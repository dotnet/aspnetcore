// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    internal class QueuePolicy : IQueuePolicy, IDisposable
    {
        private readonly int _maxConcurrentRequests;
        private readonly int _requestQueueLimit;
        private readonly SemaphoreSlim _serverSemaphore;

        private object _totalRequestsLock = new object();
        public int TotalRequests { get; private set; }

        public QueuePolicy(IOptions<QueuePolicyOptions> options)
        {
            _maxConcurrentRequests = options.Value.MaxConcurrentRequests;
            if (_maxConcurrentRequests <= 0)
            {
                throw new ArgumentException(nameof(_maxConcurrentRequests), "MaxConcurrentRequests must be a positive integer.");
            }

            _requestQueueLimit = options.Value.RequestQueueLimit;
            if (_requestQueueLimit < 0)
            {
                throw new ArgumentException(nameof(_requestQueueLimit), "The RequestQueueLimit cannot be a negative number.");
            }

            _serverSemaphore = new SemaphoreSlim(_maxConcurrentRequests);
        }

        public async ValueTask<bool> TryEnterAsync()
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

        public void OnExit()
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
