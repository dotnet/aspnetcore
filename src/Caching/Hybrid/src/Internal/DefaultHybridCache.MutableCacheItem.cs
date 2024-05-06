// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    private sealed partial class MutableCacheItem<T> : CacheItem<T> // used to hold types that require defensive copies
    {
        private IHybridCacheSerializer<T> _serializer = null!; // deferred until SetValue
        private BufferChunk _buffer;

        public void SetValue(ref BufferChunk buffer, IHybridCacheSerializer<T> serializer)
        {
            _serializer = serializer;
            _buffer = buffer;
            buffer = default; // we're taking over the lifetime; the caller no longer has it!
        }

        public void SetValue(T value, IHybridCacheSerializer<T> serializer, int maxLength)
        {
            _serializer = serializer;
            var writer = RecyclableArrayBufferWriter<byte>.Create(maxLength);
            serializer.Serialize(value, writer);

            _buffer = new(writer.DetachCommitted(out var length), length, returnToPool: true);
            writer.Dispose(); // no buffers left (we just detached them), but just in case of other logic
        }

        public override bool NeedsEvictionCallback => _buffer.ReturnToPool;

        protected override void OnFinalRelease()
        {
            DebugOnlyDecrementOutstandingBuffers();
            _buffer.RecycleIfAppropriate();
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
