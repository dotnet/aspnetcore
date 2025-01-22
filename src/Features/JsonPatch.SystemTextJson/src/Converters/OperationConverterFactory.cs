// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Converters;

internal class OperationConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType &&
               typeToConvert.GetGenericTypeDefinition() == typeof(Operation<>);
    }

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
    {
        var elementType = type.GetGenericArguments()[0];
        var converterType = typeof(OperationConverter<>).MakeGenericType(elementType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
