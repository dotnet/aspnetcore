using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RequestThrottling.Internal
{
    class CoDelQueue : RequestQueue
    {
        private const long TARGET = 5 * 10000; // 5 millis
        private const long INTERVAL = 100 * 10000; // 100 millis

        private readonly SemaphoreSlim _semaphore;

        private object _deadlineLock = new object();
        private long _deadline = 0;

        private int _consecutiveDrops = 0;

        public int TotalRequests
        {
            get => _semaphore.CurrentCount;
        }

        public CoDelQueue(int maxConcurrentRequests)
        {
            _semaphore = new SemaphoreSlim(maxConcurrentRequests);
        }

        public async Task<bool> TryEnterQueueAsync()
        {
            // =============================
            // enqueue portion
            var start = DateTime.UtcNow.Ticks;
            var waitInQueueTask = _semaphore.WaitAsync();

            if (waitInQueueTask.IsCompletedSuccessfully)
            {
                // There's room! Clear the deadline and enter the server

                lock (_deadlineLock)
                {
                    _deadline = 0;
                }

                return true;
            }
            else
            {
                await waitInQueueTask;
            }

            // ============================
            // dequeue and adjust deadline
            lock (_deadlineLock)
            {
                var now = DateTime.UtcNow.Ticks;

                if (now - start < TARGET)
                {
                    // if queue is running smoothly, clear the deadline
                    _deadline = 0;
                }
                else if (_deadline == 0)
                {
                    // if queue starts to slow, set a deadline by which it should be clear
                    _consecutiveDrops = 0;
                    _deadline = now + INTERVAL;
                }
                else if (now >= _deadline)
                {
                    // if you exceed this deadline, start dropping requests at an increasing rate
                    _consecutiveDrops++;
                    _deadline = now + ControlLaw(_consecutiveDrops);
                    _semaphore.Release();
                    return false;
                }
            }

            return true;
        }

        public long ControlLaw(int count)
        {
            return (long)(INTERVAL / Math.Sqrt(count));
        }

        public void Release()
        {
            _semaphore.Release();
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
