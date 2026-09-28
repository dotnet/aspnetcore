// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class DotNetObjectReferenceJsonConverterFactory : JsonConverterFactory
{
    public DotNetObjectReferenceJsonConverterFactory(JSRuntime jsRuntime)
    {
        JSRuntime = jsRuntime;
    }

    public JSRuntime JSRuntime { get; }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(DotNetObjectReference<>);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "We expect that types used with DotNetObjectReference are retained.")]
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions jsonSerializerOptions)
    {
        // System.Text.Json handles caching the converters per type on our behalf. No caching is required here.
        var instanceType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(DotNetObjectReferenceJsonConverter<>).MakeGenericType(instanceType);

        return (JsonConverter)Activator.CreateInstance(converterType, JSRuntime)!;
    }
}
