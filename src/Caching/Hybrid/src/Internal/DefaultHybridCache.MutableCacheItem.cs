// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    private sealed class MutableCacheItem<T> : CacheItem<T>
    {
        public MutableCacheItem(byte[] bytes, int length, IHybridCacheSerializer<T> serializer)
        {
            this.serializer = serializer;
            this.bytes = bytes;
            this.length = length;
        }

        public MutableCacheItem(T value, IHybridCacheSerializer<T> serializer, int maxLength)
        {
            this.serializer = serializer;
            using var writer = new RecyclableArrayBufferWriter<byte>(maxLength);
            serializer.Serialize(value, writer);
            bytes = writer.DetachCommitted(out length);
        }

        private readonly IHybridCacheSerializer<T> serializer;
        private readonly byte[] bytes;
        private readonly int length;

        public override T GetValue() => serializer.Deserialize(new ReadOnlySequence<byte>(bytes, 0, length));

        public override byte[]? TryGetBytes(out int length)
        {
            length = this.length;
            return bytes;
        }
    }
}
