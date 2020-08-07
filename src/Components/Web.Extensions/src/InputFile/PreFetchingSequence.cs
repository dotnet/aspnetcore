using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class PreFetchingSequence
    {
        private readonly Func<long, CancellationToken, Block> _fetchCallback;
        private readonly long _totalFetchableItems;
        private readonly int _maxBufferCapacity;
        private readonly Queue<Block> _buffer;
        private long _maxFetchedIndex;

        public PreFetchingSequence(Func<long, CancellationToken, Block> fetchCallback, long totalFetchableItems, int maxBufferCapacity)
        {
            _fetchCallback = fetchCallback;
            _totalFetchableItems = totalFetchableItems;
            _maxBufferCapacity = maxBufferCapacity;
            _buffer = new Queue<Block>();
        }

        public Block ReadNext(CancellationToken cancellationToken)
        {
            EnqueueFetches(cancellationToken);

            if (_buffer.Count == 0)
            {
                throw new InvalidOperationException("There are no more entries to read.");
            }

            var next = _buffer.Dequeue();

            EnqueueFetches(cancellationToken);

            return next;
        }

        public bool TryPeekNext(out Block result)
        {
            if (_buffer.Count > 0)
            {
                result = _buffer.Peek();
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        private void EnqueueFetches(CancellationToken cancellationToken)
        {
            while (_buffer.Count < _maxBufferCapacity && _maxFetchedIndex < _totalFetchableItems)
            {
                _buffer.Enqueue(_fetchCallback(_maxFetchedIndex++, cancellationToken));
            }
        }
    }
}
