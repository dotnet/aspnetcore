// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class DotNetCancellationTokenReferenceJsonConverter : JsonConverter<DotNetCancellationTokenReference>
{
    private readonly JSObjectReferenceJsonConverter _referenceConverter;

    public DotNetCancellationTokenReferenceJsonConverter(JSRuntime jsRuntime)
    {
        _referenceConverter = new(jsRuntime);
    }

    public override DotNetCancellationTokenReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException("A .NET cancellation token reference cannot be supplied from JavaScript.");

    public override void Write(Utf8JsonWriter writer, DotNetCancellationTokenReference value, JsonSerializerOptions options)
        => _referenceConverter.Write(writer, value.Signal, options);
}
