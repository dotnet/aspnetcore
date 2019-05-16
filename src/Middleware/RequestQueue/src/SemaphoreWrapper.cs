using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RequestQueue
{
    internal class SemaphoreWrapper
    {
        private static SemaphoreSlim semaphore;

        public SemaphoreWrapper(int qlength)
        {
            semaphore = new SemaphoreSlim(qlength);
        }

        public Task EnterQueue()
        {
            return semaphore.WaitAsync();
        }

        public void LeaveQueue()
        {
            semaphore.Release();
        }

        public int SpotsLeft
        {
            get => semaphore.CurrentCount;
        }
    }
}
