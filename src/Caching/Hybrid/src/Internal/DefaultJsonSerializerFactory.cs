// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal sealed class DefaultJsonSerializerFactory : IHybridCacheSerializerFactory
{
    public bool TryCreateSerializer<T>([NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer)
    {
        // no restriction
        serializer = new DefaultJsonSerializer<T>();
        return true;
    }

    internal sealed class DefaultJsonSerializer<T> : IHybridCacheSerializer<T>
    {
        T IHybridCacheSerializer<T>.Deserialize(ReadOnlySequence<byte> source)
        {
            var reader = new Utf8JsonReader(source);
#pragma warning disable IDE0079 // unnecessary suppression: TFM-dependent
#pragma warning disable IL2026, IL3050 // AOT bits
            return JsonSerializer.Deserialize<T>(ref reader)!;
#pragma warning restore IL2026, IL3050
#pragma warning restore IDE0079
        }

        void IHybridCacheSerializer<T>.Serialize(T value, IBufferWriter<byte> target)
        {
            using var writer = new Utf8JsonWriter(target);
#pragma warning disable IDE0079 // unnecessary suppression: TFM-dependent
#pragma warning disable IL2026, IL3050 // AOT bits
            JsonSerializer.Serialize<T>(writer, value, JsonSerializerOptions.Default);
#pragma warning restore IL2026, IL3050
#pragma warning restore IDE0079
        }
    }

}
