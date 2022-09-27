// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class JsonConverterFactoryForEnum : JsonConverterFactory
{
    private readonly JsonContext _context;

    public JsonConverterFactoryForEnum(JsonContext context)
    {
        _context = context;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    public override JsonConverter CreateConverter(
        Type typeToConvert, JsonSerializerOptions options)
    {
        var converter = (JsonConverter)Activator.CreateInstance(
            typeof(EnumConverter<>).MakeGenericType(new Type[] { typeToConvert }),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: new object[] { _context },
            culture: null)!;

        return converter;
    }
}
