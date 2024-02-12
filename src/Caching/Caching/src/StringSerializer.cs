// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;

namespace Microsoft.Extensions.Caching.Distributed;
internal sealed class StringSerializer : IReadThroughCacheSerializer<string>
{
    public string Deserialize(ReadOnlySequence<byte> source)
        => Encoding.UTF8.GetString(source);

    public void Serialize(string value, IBufferWriter<byte> target)
        => Encoding.UTF8.GetBytes(value, target);
}
