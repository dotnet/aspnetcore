// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using Grpc.Shared;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.HttpApi.Internal.Json;

internal class JsonConverterFactoryForWrappers : JsonConverterFactory
{
    private readonly JsonSettings _settings;

    public JsonConverterFactoryForWrappers(JsonSettings settings)
    {
        _settings = settings;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeof(IMessage).IsAssignableFrom(typeToConvert))
        {
            return false;
        }

        var descriptor = JsonConverterHelper.GetMessageDescriptor(typeToConvert);
        if (descriptor == null)
        {
            return false;
        }

        return ServiceDescriptorHelpers.IsWrapperType(descriptor);
    }

    public override JsonConverter CreateConverter(
        Type typeToConvert, JsonSerializerOptions options)
    {
        var converter = (JsonConverter)Activator.CreateInstance(
            typeof(WrapperConverter<>).MakeGenericType(new Type[] { typeToConvert }),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: new object[] { _settings },
            culture: null)!;

        return converter;
    }
}
