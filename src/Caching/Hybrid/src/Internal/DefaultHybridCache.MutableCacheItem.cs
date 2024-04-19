// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    private sealed partial class MutableCacheItem<T> : CacheItem<T> // used to hold types that require defensive copies
    {
        private readonly IHybridCacheSerializer<T> _serializer;
        private readonly BufferChunk _buffer;
        private int _refCount = 1; // buffer released when this becomes zero

        public MutableCacheItem(ref BufferChunk buffer, IHybridCacheSerializer<T> serializer)
        {
            _serializer = serializer;
            _buffer = buffer;
            buffer = default; // we're taking over the lifetime; the caller no longer has it!
        }

        public MutableCacheItem(T value, IHybridCacheSerializer<T> serializer, int maxLength)
        {
            _serializer = serializer;
            var writer = RecyclableArrayBufferWriter<byte>.Create(maxLength);
            serializer.Serialize(value, writer);

            _buffer = new(writer.DetachCommitted(out var length), length, returnToPool: true);
            writer.Dispose(); // no buffers left (we just detached them), but just in case of other logic
        }

        public override bool NeedsEvictionCallback => _buffer.ReturnToPool;

        public override void OnEviction() => Release();

        public override void Release()
        {
            var newCount = Interlocked.Decrement(ref _refCount);
            if (newCount == 0)
            {
                DebugDecrementOutstandingBuffers();
                _buffer.RecycleIfAppropriate();
            }
        }

        public bool TryReserve()
        {
            var oldValue = Volatile.Read(ref _refCount);
            do
            {
                if (oldValue is 0 or -1)
                {
                    return false; // already burned, or about to roll around back to zero
                }

                var updated = Interlocked.CompareExchange(ref _refCount, oldValue + 1, oldValue);
                if (updated == oldValue)
                {
                    return true; // we exchanged
                }
                oldValue = updated; // we failed, but we have an updated state
            } while (true);
        }

        public override bool TryGetValue(out T value)
        {
            if (!TryReserve()) // only if we haven't already burned
            {
                value = default!;
                return false;
            }

            try
            {
                value = _serializer.Deserialize(_buffer.AsSequence());
                return true;
            }
            finally
            {
                Release();
            }
        }

        public override bool TryReserveBuffer(out BufferChunk buffer)
        {
            if (TryReserve()) // only if we haven't already burned
            {
                buffer = _buffer.DoNotReturnToPool(); // not up to them!
                return true;
            }
            buffer = default;
            return false;
        }

        public override bool DebugIsImmutable => false;
    }
}
