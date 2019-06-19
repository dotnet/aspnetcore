using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RequestThrottling.Policies
{
    internal class StackQueuePolicy : IQueuePolicy
    {
        private readonly List<TaskCompletionSource<bool>> _buffer;
        private int _maxCapacity;
        private bool _hasReachedCapacity;
        private int _head;
        private int _queueLength;

        private static readonly Task<bool> _trueTask = Task.FromResult(true);

        private readonly object _bufferLock = new Object();

        private int _freeServerSpots;

        public StackQueuePolicy(IOptions<QueuePolicyOptions> options)
        {
            _buffer = new List<TaskCompletionSource<bool>>();
            _maxCapacity = options.Value.RequestQueueLimit;
            _freeServerSpots = options.Value.MaxConcurrentRequests;
        }

        public Task<bool> TryEnterAsync()
        {
            lock (_bufferLock)
            {
                if (_freeServerSpots > 0)
                {
                    _freeServerSpots--;
                    return _trueTask;
                }

                // if queue is full, cancel oldest request
                if (_queueLength == _maxCapacity)
                {
                    _hasReachedCapacity = true;
                    _buffer[_head].SetResult(false);
                    _queueLength--;
                }

                // enqueue request with a tcs
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (_hasReachedCapacity)
                {
                    _buffer[_head] = tcs;
                }
                else
                {
                    _buffer.Add(tcs);
                }
                _queueLength++;

                // increment _head for next time
                _head++;
                if (_head == _maxCapacity)
                {
                    _head = 0;
                }
                return tcs.Task;
            }
        }

        public void OnExit()
        {
            lock (_bufferLock)
            {
                if (_queueLength == 0)
                {
                    _freeServerSpots++;
                    return;
                }

                // step backwards and launch a new task
                if (_head == 0)
                {
                    _head = _maxCapacity - 1;
                }
                else
                {
                    _head--;
                }

                _buffer[_head].SetResult(true);
                _queueLength--;
            }
        }
    }
}
