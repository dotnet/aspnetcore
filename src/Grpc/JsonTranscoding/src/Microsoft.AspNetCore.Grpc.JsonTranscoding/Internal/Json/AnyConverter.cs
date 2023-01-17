// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Shared;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class AnyConverter<TMessage> : SettingsConverterBase<TMessage> where TMessage : IMessage, new()
{
    internal const string AnyTypeUrlField = "@type";
    internal const string AnyWellKnownTypeValueField = "value";

    public AnyConverter(JsonContext context) : base(context)
    {
    }

    public override TMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var d = JsonDocument.ParseValue(ref reader);
        if (!d.RootElement.TryGetProperty(AnyTypeUrlField, out var urlField))
        {
            throw new InvalidOperationException("Any message with no @type.");
        }

        var typeUrl = urlField.GetString();
        var typeName = Any.GetTypeName(typeUrl);

        var descriptor = Context.TypeRegistry.Find(typeName);
        if (descriptor == null)
        {
            throw new InvalidOperationException($"Type registry has no descriptor for type name '{typeName}'.");
        }

        // Ensure the payload descriptor is registered. It's possible the payload type isn't in a proto referenced by the service, and is only in the user-specified TypeRegistry.
        // There isn't a way to enumerate the contents of the TypeRegistry so we have to ensure the descriptor is present every time.
        Context.DescriptorRegistry.RegisterFileDescriptor(descriptor.File);

        IMessage data;
        if (ServiceDescriptorHelpers.IsWellKnownType(descriptor))
        {
            if (!d.RootElement.TryGetProperty(AnyWellKnownTypeValueField, out var valueField))
            {
                throw new InvalidOperationException($"Expected '{AnyWellKnownTypeValueField}' property for well-known type Any body.");
            }

            data = (IMessage)JsonSerializer.Deserialize(valueField, descriptor.ClrType, options)!;
        }
        else
        {
            data = (IMessage)JsonSerializer.Deserialize(d.RootElement, descriptor.ClrType, options)!;
        }

        var message = new TMessage();
        message.Descriptor.Fields[Any.TypeUrlFieldNumber].Accessor.SetValue(message, typeUrl);
        message.Descriptor.Fields[Any.ValueFieldNumber].Accessor.SetValue(message, data.ToByteString());

        return message;
    }

    public override void Write(Utf8JsonWriter writer, TMessage value, JsonSerializerOptions options)
    {
        var typeUrl = (string)value.Descriptor.Fields[Any.TypeUrlFieldNumber].Accessor.GetValue(value);
        var data = (ByteString)value.Descriptor.Fields[Any.ValueFieldNumber].Accessor.GetValue(value);
        var typeName = Any.GetTypeName(typeUrl);
        var descriptor = Context.TypeRegistry.Find(typeName);
        if (descriptor == null)
        {
            throw new InvalidOperationException($"Type registry has no descriptor for type name '{typeName}'.");
        }

        // Ensure the payload descriptor is registered. It's possible the payload type isn't in a proto referenced by the service, and is only in the user-specified TypeRegistry.
        // There isn't a way to enumerate the contents of the TypeRegistry so we have to ensure the descriptor is present every time.
        Context.DescriptorRegistry.RegisterFileDescriptor(descriptor.File);
        
        var valueMessage = descriptor.Parser.ParseFrom(data);
        writer.WriteStartObject();
        writer.WriteString(AnyTypeUrlField, typeUrl);

        if (ServiceDescriptorHelpers.IsWellKnownType(descriptor))
        {
            writer.WritePropertyName(AnyWellKnownTypeValueField);
            if (ServiceDescriptorHelpers.IsWrapperType(descriptor))
            {
                var wrappedValue = valueMessage.Descriptor.Fields[JsonConverterHelper.WrapperValueFieldNumber].Accessor.GetValue(valueMessage);
                JsonSerializer.Serialize(writer, wrappedValue, wrappedValue.GetType(), options);
            }
            else
            {
                JsonSerializer.Serialize(writer, valueMessage, valueMessage.GetType(), options);
            }
        }
        else
        {
            WriteMessageFields(writer, valueMessage, Context.Settings, options);
        }

        writer.WriteEndObject();
    }

    internal static void WriteMessageFields(Utf8JsonWriter writer, IMessage message, GrpcJsonSettings settings, JsonSerializerOptions options)
    {
        var fields = message.Descriptor.Fields;

        foreach (var field in fields.InFieldNumberOrder())
        {
            var accessor = field.Accessor;
            var value = accessor.GetValue(message);
            if (!JsonConverterHelper.ShouldFormatFieldValue(message, field, value, !settings.IgnoreDefaultValues))
            {
                continue;
            }

            writer.WritePropertyName(accessor.Descriptor.JsonName);
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
