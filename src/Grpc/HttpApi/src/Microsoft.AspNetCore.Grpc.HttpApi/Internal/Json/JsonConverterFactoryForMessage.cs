// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.HttpApi.Internal.Json;

internal class JsonConverterFactoryForMessage : JsonConverterFactory
{
    private readonly JsonSettings _settings;

    public JsonConverterFactoryForMessage(JsonSettings settings)
    {
        _settings = settings;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IMessage).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter CreateConverter(
        Type typeToConvert, JsonSerializerOptions options)
    {
        JsonConverter converter = (JsonConverter)Activator.CreateInstance(
            typeof(MessageConverter<>).MakeGenericType(new Type[] { typeToConvert }),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: new object[] { _settings },
            culture: null)!;

        return converter;
    }
}
