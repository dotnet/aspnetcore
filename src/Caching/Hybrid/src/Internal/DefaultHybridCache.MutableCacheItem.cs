// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    private sealed class MutableCacheItem<T> : CacheItem<T> // used to hold types that require defensive copies
    {
        public MutableCacheItem(byte[] bytes, int length, IHybridCacheSerializer<T> serializer)
        {
            _serializer = serializer;
            _bytes = bytes;
            _length = length;
        }

        public MutableCacheItem(T value, IHybridCacheSerializer<T> serializer, int maxLength)
        {
            _serializer = serializer;
            var writer = RecyclableArrayBufferWriter<byte>.Create(maxLength);
            serializer.Serialize(value, writer);
            _bytes = writer.DetachCommitted(out _length);
            writer.Dispose(); // only recycle on success
        }

        private readonly IHybridCacheSerializer<T> _serializer;
        private readonly byte[] _bytes;
        private readonly int _length;

        public override T GetValue() => _serializer.Deserialize(new ReadOnlySequence<byte>(_bytes, 0, _length));

        public override byte[]? TryGetBytes(out int length)
        {
            length = _length;
            return _bytes;
        }
    }
}
