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
        private readonly int _maxTotalRequest;
        private readonly SemaphoreSlim _serverSemaphore;

        private object _totalRequestsLock = new object();

        private readonly static ValueTask<bool> _falseValueTask = new ValueTask<bool>(false);

        public int TotalRequests { get; private set; }

        public QueuePolicy(IOptions<QueuePolicyOptions> options)
        {
            if (options.Value.MaxConcurrentRequests <= 0)
            {
                throw new ArgumentException(nameof(options.Value.MaxConcurrentRequests), "MaxConcurrentRequests must be a positive integer.");
            }

            if (options.Value.RequestQueueLimit < 0)
            {
                throw new ArgumentException(nameof(options.Value.RequestQueueLimit), "The RequestQueueLimit cannot be a negative number.");
            }

            _serverSemaphore = new SemaphoreSlim(options.Value.MaxConcurrentRequests);

            _maxTotalRequest = options.Value.MaxConcurrentRequests + options.Value.RequestQueueLimit;
        }

        public ValueTask<bool> TryEnterAsync()
        {
            // a return value of 'false' indicates that the request is rejected
            // a return value of 'true' indicates that the request may proceed
            // _serverSemaphore.Release is *not* called in this method, it is called externally when requests leave the server

            lock (_totalRequestsLock)
            {
                if (TotalRequests >= _maxTotalRequest)
                {
                    return _falseValueTask;
                }

                TotalRequests++;
            }

            return AwaitSemaphore();
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

        private async ValueTask<bool> AwaitSemaphore()
        {
            await _serverSemaphore.WaitAsync();

            return true;
        }
    }
}
