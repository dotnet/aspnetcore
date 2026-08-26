// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.JSInterop.Infrastructure;

// JS interop's custom converters own the complete JSON contract for their supported types. Supply
// converter-backed metadata so applications don't need to register each closed generic or
// implementation type with a source-generated context when reflection-based serialization is
// disabled. IJSVoidResult is also framework-owned and only ever deserializes the JSON null value.
internal sealed class JSInteropJsonTypeInfoResolver : IJsonTypeInfoResolver
{
    public static readonly JSInteropJsonTypeInfoResolver Instance = new();

    private JSInteropJsonTypeInfoResolver()
    {
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "JS interop types are handled by custom converters that don't serialize object graphs.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "JS interop types are handled by custom converters that don't require runtime code generation.")]
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        if (type == typeof(IJSVoidResult))
        {
            return JsonTypeInfo.CreateJsonTypeInfo(type, options);
        }

        foreach (var converter in options.Converters)
        {
            if (converter.CanConvert(type))
            {
                return JsonTypeInfo.CreateJsonTypeInfo(type, options);
            }
        }

        return null;
    }
}
