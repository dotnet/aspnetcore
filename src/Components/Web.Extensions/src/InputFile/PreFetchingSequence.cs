using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class PreFetchingSequence
    {
        private readonly Func<long, CancellationToken, EncodedFileChunk> _fetchCallback;
        private readonly long _totalFetchableItems;
        private readonly int _maxBufferCapacity;
        private readonly Queue<EncodedFileChunk> _buffer;
        private long _maxFetchedIndex;

        public PreFetchingSequence(Func<long, CancellationToken, EncodedFileChunk> fetchCallback, long totalFetchableItems, int maxBufferCapacity)
        {
            _fetchCallback = fetchCallback;
            _totalFetchableItems = totalFetchableItems;
            _maxBufferCapacity = maxBufferCapacity;
            _buffer = new Queue<EncodedFileChunk>();
        }

        public EncodedFileChunk ReadNext(CancellationToken cancellationToken)
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

        public bool TryPeekNext(out EncodedFileChunk result)
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
