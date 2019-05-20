using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RequestThrottling
{
    public class SemaphoreWrapper : IDisposable
    {
        private SemaphoreSlim _semaphore;
        public int TotalRequests { get; set; } = 0;

        public SemaphoreWrapper(int queueLength)
        {
            TotalRequests += 1;
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
