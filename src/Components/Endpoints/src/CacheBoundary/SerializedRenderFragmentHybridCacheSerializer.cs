// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;

namespace Microsoft.AspNetCore.Components.Endpoints;

// Serializer used by HybridCache when a CacheBoundary payload is stored as a SerializedRenderFragment
// object instead of a pre-serialized byte[]. Reuses the same JSON options as the rest of the component
// serialization pipeline so the cached shape matches what RenderFragmentSerializer expects on read.
internal sealed class SerializedRenderFragmentHybridCacheSerializer : IHybridCacheSerializer<SerializedRenderFragment>
{
    public static readonly SerializedRenderFragmentHybridCacheSerializer Instance = new();

    public SerializedRenderFragment Deserialize(ReadOnlySequence<byte> source)
    {
        var reader = new Utf8JsonReader(source);
        return JsonSerializer.Deserialize<SerializedRenderFragment>(ref reader, ServerComponentSerializationSettings.JsonSerializationOptions)
            ?? new SerializedRenderFragment();
    }

    public void Serialize(SerializedRenderFragment value, IBufferWriter<byte> target)
    {
        using var writer = new Utf8JsonWriter(target);
        JsonSerializer.Serialize(writer, value, ServerComponentSerializationSettings.JsonSerializationOptions);
    }
}
