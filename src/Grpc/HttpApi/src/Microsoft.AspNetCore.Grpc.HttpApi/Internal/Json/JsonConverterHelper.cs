// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Shared;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.HttpApi.Internal.Json;

internal static class JsonConverterHelper
{
    internal const int WrapperValueFieldNumber = Int32Value.ValueFieldNumber;

    internal static JsonSerializerOptions CreateSerializerOptions(JsonSettings settings, bool isStreamingOptions = false)
    {
        // Streaming is line delimited between messages. That means JSON can't be indented as it adds new lines.
        // For streaming to work, indenting must be disabled when streaming.
        var writeIndented = !isStreamingOptions ? settings.WriteIndented : false;

        var options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        options.Converters.Add(new NullValueConverter());
        options.Converters.Add(new ByteStringConverter());
        options.Converters.Add(new Int64Converter(settings));
        options.Converters.Add(new UInt64Converter());
        options.Converters.Add(new BoolConverter());
        options.Converters.Add(new JsonConverterFactoryForEnum(settings));
        options.Converters.Add(new JsonConverterFactoryForWrappers(settings));
        options.Converters.Add(new JsonConverterFactoryForWellKnownTypes(settings));
        options.Converters.Add(new JsonConverterFactoryForMessage(settings));

        return options;
    }

    internal static Type GetFieldType(FieldDescriptor descriptor)
    {
        switch (descriptor.FieldType)
        {
            case FieldType.Bool:
                return typeof(bool);
            case FieldType.Bytes:
                return typeof(ByteString);
            case FieldType.String:
                return typeof(string);
            case FieldType.Double:
                return typeof(double);
            case FieldType.SInt32:
            case FieldType.Int32:
            case FieldType.SFixed32:
                return typeof(int);
            case FieldType.Enum:
                return descriptor.EnumType.ClrType;
            case FieldType.Fixed32:
            case FieldType.UInt32:
                return typeof(uint);
            case FieldType.Fixed64:
            case FieldType.UInt64:
                return typeof(ulong);
            case FieldType.SFixed64:
            case FieldType.Int64:
            case FieldType.SInt64:
                return typeof(long);
            case FieldType.Float:
                return typeof(float);
            case FieldType.Message:
            case FieldType.Group: // Never expect to get this, but...
                if (ServiceDescriptorHelpers.IsWrapperType(descriptor.MessageType))
                {
                    var t = GetFieldType(descriptor.MessageType.Fields[WrapperValueFieldNumber]);
                    if (t.IsValueType)
                    {
                        return typeof(Nullable<>).MakeGenericType(t);
                    }

                    return t;
                }
                return descriptor.MessageType.ClrType;
            default:
                throw new ArgumentException("Invalid field type");
        }
    }

    internal static MessageDescriptor? GetMessageDescriptor(Type typeToConvert)
    {
        var property = typeToConvert.GetProperty("Descriptor", BindingFlags.Static | BindingFlags.Public, binder: null, typeof(MessageDescriptor), Type.EmptyTypes, modifiers: null);
        if (property == null)
        {
            return null;
        }

        return property.GetValue(null, null) as MessageDescriptor;
    }

    public static void PopulateMap(ref Utf8JsonReader reader, JsonSerializerOptions options, IMessage message, FieldDescriptor fieldDescriptor)
    {
        var mapFields = fieldDescriptor.MessageType.Fields.InFieldNumberOrder();
        var mapKey = mapFields[0];
        var mapValue = mapFields[1];

        var keyType = GetFieldType(mapKey);
        var valueType = GetFieldType(mapValue);

        var repeatedFieldType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        var newValues = (IDictionary)JsonSerializer.Deserialize(ref reader, repeatedFieldType, options)!;

        var existingValue = (IDictionary)fieldDescriptor.Accessor.GetValue(message);
        foreach (DictionaryEntry item in newValues)
        {
            existingValue[item.Key] = item.Value;
        }
    }

    public static void PopulateList(ref Utf8JsonReader reader, JsonSerializerOptions options, IMessage message, FieldDescriptor fieldDescriptor)
    {
        var fieldType = GetFieldType(fieldDescriptor);
        var repeatedFieldType = typeof(List<>).MakeGenericType(fieldType);
        var newValues = (IList)JsonSerializer.Deserialize(ref reader, repeatedFieldType, options)!;

        var existingValue = (IList)fieldDescriptor.Accessor.GetValue(message);
        foreach (var item in newValues)
        {
            existingValue.Add(item);
        }
    }
}
