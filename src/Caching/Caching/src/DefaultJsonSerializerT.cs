// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text.Json;

namespace Microsoft.Extensions.Caching.Distributed;
internal sealed class DefaultJsonSerializer<T> : ICacheSerializer<T>
{
    T ICacheSerializer<T>.Deserialize(ReadOnlySequence<byte> source)
    {
        Utf8JsonReader reader = new Utf8JsonReader(source);
#pragma warning disable IL2026, IL3050 // AOT bits
        return JsonSerializer.Deserialize<T>(ref reader)!;
#pragma warning restore IL2026, IL3050
    }

    void ICacheSerializer<T>.Serialize(T value, IBufferWriter<byte> target)
    {
        using var writer = new Utf8JsonWriter(target);
#pragma warning disable IL2026, IL3050 // AOT bits
        JsonSerializer.Serialize<T>(writer, value, JsonSerializerOptions.Default);
#pragma warning restore IL2026, IL3050
    }
}
