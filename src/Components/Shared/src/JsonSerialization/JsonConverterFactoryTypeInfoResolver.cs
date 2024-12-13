// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components;

// For custom converters that don't rely on serializing an object graph,
// we can resolve the incoming type's JsonTypeInfo directly from the converter.
// This skips extra work to collect metadata for the type that won't be used.
internal sealed class JsonConverterFactoryTypeInfoResolver<T> : IJsonTypeInfoResolver
{
    public static readonly JsonConverterFactoryTypeInfoResolver<T> Instance = new();

    private JsonConverterFactoryTypeInfoResolver()
    {
    }

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        if (type != typeof(T))
        {
            return null;
        }

        foreach (var converter in options.Converters)
        {
            if (converter is not JsonConverterFactory factory || !factory.CanConvert(type))
            {
                continue;
            }

            if (factory.CreateConverter(type, options) is not { } converterToUse)
            {
                continue;
            }

            return JsonMetadataServices.CreateValueInfo<T>(options, converterToUse);
        }

        return null;
    }
}
