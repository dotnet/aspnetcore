// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class JsonConverterFactoryForWellKnownTypes : JsonConverterFactory
{
    private readonly JsonContext _context;

    public JsonConverterFactoryForWellKnownTypes(JsonContext context)
    {
        _context = context;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeof(IMessage).IsAssignableFrom(typeToConvert))
        {
            return false;
        }

        var descriptor = _context.DescriptorRegistry.FindDescriptorByType(typeToConvert);
        if (descriptor == null)
        {
            return false;
        }

        return JsonConverterHelper.WellKnownTypeNames.ContainsKey(descriptor.FullName);
    }

    public override JsonConverter CreateConverter(
        Type typeToConvert, JsonSerializerOptions options)
    {
        var descriptor = _context.DescriptorRegistry.FindDescriptorByType(typeToConvert)!;
        var converterType = JsonConverterHelper.WellKnownTypeNames[descriptor.FullName];

        var converter = (JsonConverter)Activator.CreateInstance(
            converterType.MakeGenericType(new Type[] { typeToConvert }),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: new object[] { _context },
            culture: null)!;

        return converter;
    }
}
