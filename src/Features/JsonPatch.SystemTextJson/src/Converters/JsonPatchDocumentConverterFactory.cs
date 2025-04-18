// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Converters;

internal class JsonPatchDocumentConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(JsonPatchDocument) ||
               (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(JsonPatchDocument<>));
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(JsonPatchDocument))
        {
            return new JsonPatchDocumentConverter();
        }

        return (JsonConverter)Activator.CreateInstance(typeof(JsonConverterForJsonPatchDocumentOfT<>).MakeGenericType(typeToConvert.GenericTypeArguments[0]));
    }
}
