using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RequestThrottling.Policies
{
    class StackPolicy : IQueuePolicy
    {
        private TaskCompletionSource<bool>[] _buffer;
        private int _head; // does both writes and reads
        private int _queueLength; // tracks the back of the queue

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

                if (_queueLength == _buffer.Length)
                {
                    _buffer[_head].SetResult(false);
                    _queueLength--;
                }

                // make a new task, place it in current spot
                var tcs = new TaskCompletionSource<bool>();
                _buffer[_head] = tcs;
                _queueLength++;

                // increment _head for next time
                _head = (_head + 1) % _buffer.Length;

                return tcs.Task;
            }
        }

        public void OnExit()
        {
            // going backwards,
            // find a task you can run, and run it
            //     (this is find a waiting task, and set its result to True)

            lock (_bufferLock)
            {
                if (_queueLength == 0)
                {
                    _freeServerSpots++;
                    return;
                }

                _head = (_head - 1) % _buffer.Length;
                _buffer[_head].SetResult(true);
                _queueLength--;
            }
        }
    }
}
