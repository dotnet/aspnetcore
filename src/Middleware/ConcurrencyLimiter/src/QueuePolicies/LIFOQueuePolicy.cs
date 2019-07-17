using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    internal class LIFOQueuePolicy : IQueuePolicy
    {
        private readonly List<ResettableBooleanTCS> _buffer;
        private readonly int _maxQueueCapacity;
        private readonly int _maxConcurrentRequests;
        private bool _hasReachedCapacity;
        private int _head;
        private int _queueLength;

        private readonly object _bufferLock = new Object();

        private int _freeServerSpots;

        public LIFOQueuePolicy(IOptions<QueuePolicyOptions> options)
        {
            _buffer = new List<ResettableBooleanTCS>();
            _maxQueueCapacity = options.Value.RequestQueueLimit;
            _maxConcurrentRequests = options.Value.MaxConcurrentRequests;
            _freeServerSpots = options.Value.MaxConcurrentRequests;
        }

        public async Task<bool> TryEnterAsync()
        {
            ResettableBooleanTCS tcs;

            lock (_bufferLock)
            {
                if (_freeServerSpots > 0)
                {
                    _freeServerSpots--;
                    return true;
                }

                // if queue is full, cancel oldest request
                if (_queueLength == _maxQueueCapacity)
                {
                    _hasReachedCapacity = true;
                    _buffer[_head].CompleteFalse();
                    _queueLength--;
                }

                // enqueue request with a tcs
                //var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                if (!_hasReachedCapacity && _queueLength >= _buffer.Count)
                {
                    _buffer.Add(new ResettableBooleanTCS());
                }

                tcs = _buffer[_head];
                _queueLength++;

                // increment _head for next time
                _head++;
                if (_head == _maxQueueCapacity)
                {
                    _head = 0;
                }
            }

            return await tcs;
        }

        public void OnExit()
        {
            lock (_bufferLock)
            {
                if (_queueLength == 0)
                {
                    _freeServerSpots++;

                    if (_freeServerSpots > _maxConcurrentRequests)
                    {
                        _freeServerSpots--;
                        throw new InvalidOperationException("OnExit must only be called once per successful call to TryEnterAsync");
                    }

                    return;
                }

                // step backwards and launch a new task
                if (_head == 0)
                {
                    _head = _maxQueueCapacity - 1;
                }
                else
                {
                    _head--;
                }

                _buffer[_head].CompleteTrue();
                _queueLength--;
            }
        }
    }
}
