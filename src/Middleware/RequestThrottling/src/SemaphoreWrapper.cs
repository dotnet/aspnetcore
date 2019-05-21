// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RequestThrottling
{
    internal class SemaphoreWrapper : IDisposable
    {
        private SemaphoreSlim _semaphore;

        public SemaphoreWrapper(int queueLength)
        {
            _semaphore = new SemaphoreSlim(queueLength);
        }

        public Task EnterQueue()
        {
            return _semaphore.WaitAsync();
        }

        public void LeaveQueue()
        {
            _semaphore.Release();
        }

        public int Count
        {
            get => _semaphore.CurrentCount;
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
