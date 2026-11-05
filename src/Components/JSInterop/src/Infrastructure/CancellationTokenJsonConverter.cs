// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class CancellationTokenJsonConverter : JsonConverter<CancellationToken>
{
    private readonly DotNetObjectReferenceJsonConverter<JSCancellationTokenReference> _referenceConverter;

    public CancellationTokenJsonConverter(JSRuntime jsRuntime)
    {
        _referenceConverter = new(jsRuntime);
    }

    public override CancellationToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var reference = _referenceConverter.Read(ref reader, typeof(DotNetObjectReference<JSCancellationTokenReference>), options);
        return reference.Value.Token;
    }

    public override void Write(Utf8JsonWriter writer, CancellationToken value, JsonSerializerOptions options)
        => throw new NotSupportedException($"Use {nameof(DotNetCancellationTokenReference)}.{nameof(DotNetCancellationTokenReference.CreateAsync)} to pass a cancellation token to JavaScript with an explicit reference lifetime.");
}
