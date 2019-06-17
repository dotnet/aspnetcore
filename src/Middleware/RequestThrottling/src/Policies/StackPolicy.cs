using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RequestThrottling.Policies
{
    class StackPolicy : IQueuePolicy
    {
        private TaskCompletionSource<bool>[] _buffer;
        private int _head; 
        private int _queueLength; 

        private object _bufferLock = new Object();

        private int _freeServerSpots;

        public StackPolicy(IOptions<TailDropOptions> options)
        {
            _buffer = new TaskCompletionSource<bool>[options.Value.RequestQueueLimit];
            _freeServerSpots = options.Value.MaxConcurrentRequests;
        }

        public Task<bool> TryEnterAsync()
        {
            lock (_bufferLock)
            {
                if (_freeServerSpots > 0)
                {
                    _freeServerSpots--;
                    return Task.FromResult(true);
                }

                // if queue is full, cancel oldest request
                if (_queueLength == _buffer.Length)
                {
                    _buffer[_head].SetResult(false);
                    _queueLength--;
                }

                // enqueue request with a tcs
                var tcs = new TaskCompletionSource<bool>();
                _buffer[_head] = tcs;
                _queueLength++;

                // increment _head for next time
                if (_head++ == _buffer.Length - 1)
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
                if (_head-- == 0)
                {
                    _head = _buffer.Length - 1;
                }
                _buffer[_head].SetResult(true);
                _queueLength--;
            }
        }
    }
}
