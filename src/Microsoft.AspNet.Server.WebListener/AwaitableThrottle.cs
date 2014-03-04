//------------------------------------------------------------------------------
// <copyright file="HttpListener.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.WebListener
{
    /// <summary>
    /// Awaitable object that acts like a semaphore. The object would wait if more than maxConcurrent number of clients waits on it
    /// </summary>
    public class AwaitableThrottle
    {
        private static readonly TaskAwaiter<bool> CompletedAwaiter = Task.FromResult(true).GetAwaiter();

        private int _maxConcurrent;
        private readonly object _thislock;
        private readonly Queue<TaskCompletionSource<bool>> _awaiters;

        private int _count;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="maxConcurrent">maximum number of clients that can wait on this object at the same time</param>
        public AwaitableThrottle(int maxConcurrent)
        {
            _thislock = new object();
            _awaiters = new Queue<TaskCompletionSource<bool>>();
            _maxConcurrent = maxConcurrent;
        }

        /// <summary>
        /// Maximum amount of clients who can await on this throttle
        /// </summary>
        public int MaxConcurrent
        {
            get
            {
                return _maxConcurrent;
            }
            set
            {
                // Note: non-thread safe
                _maxConcurrent = value;
            }
        }

        /// <summary>
        /// Called by framework
        /// </summary>
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

        /// <summary>
        /// Release throttle
        /// </summary>
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
