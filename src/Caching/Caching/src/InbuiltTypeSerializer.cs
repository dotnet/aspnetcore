// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;

namespace Microsoft.Extensions.Caching.Distributed;
internal sealed class InbuiltTypeSerializer : IHybridCacheSerializer<string>, IHybridCacheSerializer<byte[]>
{
    private static InbuiltTypeSerializer? _instance;
    public static InbuiltTypeSerializer Instance => _instance ??= new();
    string IHybridCacheSerializer<string>.Deserialize(ReadOnlySequence<byte> source)
        => Encoding.UTF8.GetString(source);

    void IHybridCacheSerializer<string>.Serialize(string value, IBufferWriter<byte> target)
        => Encoding.UTF8.GetBytes(value, target);

    byte[] IHybridCacheSerializer<byte[]>.Deserialize(ReadOnlySequence<byte> source)
        => source.ToArray();

    void IHybridCacheSerializer<byte[]>.Serialize(byte[] value, IBufferWriter<byte> target)
        => target.Write(value);
}
