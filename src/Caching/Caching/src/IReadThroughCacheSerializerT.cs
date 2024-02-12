// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.Extensions.Caching.Distributed;

public interface IReadThroughCacheSerializer<T>
{
    T Deserialize(ReadOnlySequence<byte> source);
    void Serialize(T value, IBufferWriter<byte> target);
}

