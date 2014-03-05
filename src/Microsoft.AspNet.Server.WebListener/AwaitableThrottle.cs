using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.WebListener
{
    /// <summary>
    /// AwaitableThrottle is an awaitable object that acts like a semaphore. The object would wait if more than maxConcurrent number of clients waits on it.
    /// </summary>
    internal class AwaitableThrottle
    {
        private static readonly TaskAwaiter<bool> CompletedAwaiter = Task.FromResult(true).GetAwaiter();

        private int _maxConcurrent;
        private readonly object _thislock;
        private readonly Queue<TaskCompletionSource<bool>> _awaiters;

        private int _count;

        // <param name="maxConcurrent">Maximum number of clients that can wait on this object at the same time.</param>
        public AwaitableThrottle(int maxConcurrent)
        {
            _thislock = new object();
            _awaiters = new Queue<TaskCompletionSource<bool>>();
            _maxConcurrent = maxConcurrent;
        }

        public int MaxConcurrent
        {
            get
            {
                return _maxConcurrent;
            }
            set
            {
                Contract.Assert(_maxConcurrent >= 0,
                    "Behavior of this class is undefined for negative value");
                // Note:
                // 1. This setter is non-thread safe. We assumed it doesnt need to be for simplicity sake.
                // 2. Behavior of this class is not well defined if a negative value is passed in. If it 
                //    is awaited before any Release() is called, the subsequent Relese() would eagerly
                //    unblock awaiting thread instead of waiting for _count to reach the negative value specified.
                _maxConcurrent = value;
            }
        }

        public TaskAwaiter<bool> GetAwaiter()
        {
            TaskCompletionSource<bool> awaiter;

            lock (_thislock)
            {
                if (_count < _maxConcurrent)
                {
                    _count++;
                    return CompletedAwaiter;
                }

                awaiter = new TaskCompletionSource<bool>();
                _awaiters.Enqueue(awaiter);
            }

            return awaiter.Task.GetAwaiter();
        }

        public void Release()
        {
            TaskCompletionSource<bool> completion = null;

            lock (_thislock)
            {
                if (_awaiters.Count > 0)
                {
                    completion = _awaiters.Dequeue();
                }
                else
                {
                    _count--;
                }
            }

            if (completion != null)
            {
                completion.SetResult(true);
            }
        }
    }  
}
