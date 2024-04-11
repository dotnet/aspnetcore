// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal sealed class InbuiltTypeSerializer : IHybridCacheSerializer<string>, IHybridCacheSerializer<byte[]>
{
    public static InbuiltTypeSerializer Instance { get; } = new();

    string IHybridCacheSerializer<string>.Deserialize(ReadOnlySequence<byte> source)
    {
#if NET5_0_OR_GREATER
        return Encoding.UTF8.GetString(source);
#else
        if (source.IsSingleSegment && MemoryMarshal.TryGetArray(source.First, out var segment))
        {
            // we can use the existing single chunk as-is
            return Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
        }

        var length = checked((int)source.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(length);
        source.CopyTo(oversized);
        var s = Encoding.UTF8.GetString(oversized, 0, length);
        ArrayPool<byte>.Shared.Return(oversized);
        return s;
#endif
    }

    void IHybridCacheSerializer<string>.Serialize(string value, IBufferWriter<byte> target)
    {
#if NET5_0_OR_GREATER
        Encoding.UTF8.GetBytes(value, target);
#else
        var length = Encoding.UTF8.GetByteCount(value);
        var oversized = ArrayPool<byte>.Shared.Rent(length);
        var actual = Encoding.UTF8.GetBytes(value, 0, value.Length, oversized, 0);
        Debug.Assert(actual == length);
        target.Write(new(oversized, 0, length));
        ArrayPool<byte>.Shared.Return(oversized);
#endif
    }

    byte[] IHybridCacheSerializer<byte[]>.Deserialize(ReadOnlySequence<byte> source)
        => source.ToArray();

    void IHybridCacheSerializer<byte[]>.Serialize(byte[] value, IBufferWriter<byte> target)
        => target.Write(value);
}
