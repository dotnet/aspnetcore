// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components;

internal sealed class JsonConverterFactoryTypeInfoResolver : IJsonTypeInfoResolver
{
    private static readonly MethodInfo _createValueInfoMethod = ((Delegate)JsonMetadataServices.CreateValueInfo<object>).Method.GetGenericMethodDefinition();

    public static readonly JsonConverterFactoryTypeInfoResolver Instance = new();

    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "We expect the incoming type to have already been correctly preserved")]
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        foreach (var converter in options.Converters)
        {
            if (converter is not JsonConverterFactory factory || !factory.CanConvert(type))
            {
                continue;
            }

            var converterToUse = factory.CreateConverter(type, options);
            var createValueInfo = _createValueInfoMethod.MakeGenericMethod(type);

            if (createValueInfo.Invoke(null, [options, converterToUse]) is not JsonTypeInfo jsonTypeInfo)
            {
                throw new InvalidOperationException($"Unable to create a {nameof(JsonTypeInfo)} for the type {type.FullName}");
            }

            return jsonTypeInfo;
        }

        return null;
    }
}
