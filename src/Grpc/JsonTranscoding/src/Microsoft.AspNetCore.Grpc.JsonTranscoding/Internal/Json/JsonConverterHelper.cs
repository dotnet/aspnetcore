// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Shared;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal static class JsonConverterHelper
{
    internal const int WrapperValueFieldNumber = Int32Value.ValueFieldNumber;

    internal static readonly Dictionary<string, Type> WellKnownTypeNames = new Dictionary<string, Type>
    {
        [Any.Descriptor.FullName] = typeof(AnyConverter<>),
        [Duration.Descriptor.FullName] = typeof(DurationConverter<>),
        [Timestamp.Descriptor.FullName] = typeof(TimestampConverter<>),
        [FieldMask.Descriptor.FullName] = typeof(FieldMaskConverter<>),
        [Struct.Descriptor.FullName] = typeof(StructConverter<>),
        [ListValue.Descriptor.FullName] = typeof(ListValueConverter<>),
        [Value.Descriptor.FullName] = typeof(ValueConverter<>),
    };

    internal static JsonSerializerOptions CreateSerializerOptions(JsonContext context, bool isStreamingOptions = false)
    {
        // Streaming is line delimited between messages. That means JSON can't be indented as it adds new lines.
        // For streaming to work, indenting must be disabled when streaming.
        var writeIndented = !isStreamingOptions ? context.Settings.WriteIndented : false;

        var typeInfoResolver = JsonTypeInfoResolver.Combine(
            new MessageTypeInfoResolver(context),
            new DefaultJsonTypeInfoResolver());

        var options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = typeInfoResolver
        };
        options.Converters.Add(new NullValueConverter());
        options.Converters.Add(new ByteStringConverter());
        options.Converters.Add(new Int64Converter(context));
        options.Converters.Add(new UInt64Converter(context));
        options.Converters.Add(new BoolConverter());
        options.Converters.Add(new JsonConverterFactoryForEnum(context));
        options.Converters.Add(new JsonConverterFactoryForWrappers(context));
        options.Converters.Add(new JsonConverterFactoryForWellKnownTypes(context));

        return options;
    }

    internal static Type GetFieldType(FieldDescriptor descriptor)
    {
        if (descriptor.IsMap)
        {
            var mapFields = descriptor.MessageType.Fields.InFieldNumberOrder();
            var keyField = mapFields[0];
            var valueField = mapFields[1];

            return typeof(MapField<,>).MakeGenericType(GetFieldTypeCore(keyField), GetFieldTypeCore(valueField));
        }
        else if (descriptor.IsRepeated)
        {
            var itemType = GetFieldTypeCore(descriptor);

            return typeof(RepeatedField<>).MakeGenericType(itemType);
        }
        else
        {
            // Return nullable field types so the serializer can successfully deserialize null value.
            return GetFieldTypeCore(descriptor, nullableType: true);
        }
    }

    private static Type GetFieldTypeCore(FieldDescriptor descriptor, bool nullableType = false)
    {
        switch (descriptor.FieldType)
        {
            case FieldType.Bool:
                return nullableType ? typeof(bool?) : typeof(bool);
            case FieldType.Bytes:
                return typeof(ByteString);
            case FieldType.String:
                return typeof(string);
            case FieldType.Double:
                return nullableType ? typeof(double?) : typeof(double);
            case FieldType.SInt32:
            case FieldType.Int32:
            case FieldType.SFixed32:
                return nullableType ? typeof(int?) : typeof(int);
            case FieldType.Enum:
                return nullableType ? typeof(Nullable<>).MakeGenericType(descriptor.EnumType.ClrType) : descriptor.EnumType.ClrType;
            case FieldType.Fixed32:
            case FieldType.UInt32:
                return nullableType ? typeof(uint?) : typeof(uint);
            case FieldType.Fixed64:
            case FieldType.UInt64:
                return nullableType ? typeof(ulong?) : typeof(ulong);
            case FieldType.SFixed64:
            case FieldType.Int64:
            case FieldType.SInt64:
                return nullableType ? typeof(long?) : typeof(long);
            case FieldType.Float:
                return nullableType ? typeof(float?) : typeof(float);
            case FieldType.Message:
            case FieldType.Group: // Never expect to get this, but...
                if (ServiceDescriptorHelpers.IsWrapperType(descriptor.MessageType))
                {
                    return GetFieldType(descriptor.MessageType.Fields[WrapperValueFieldNumber]);
                }

                return descriptor.MessageType.ClrType;
            default:
                throw new ArgumentException("Invalid field type");
        }
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
        var itemType = fieldType.GetGenericArguments()[0];
        var repeatedFieldType = typeof(List<>).MakeGenericType(itemType);
        var newValues = (IList)JsonSerializer.Deserialize(ref reader, repeatedFieldType, options)!;

        var existingValue = (IList)fieldDescriptor.Accessor.GetValue(message);
        foreach (var item in newValues)
        {
            existingValue.Add(item);
        }
    }

    /// <summary>
    /// Determines whether or not a field value should be serialized according to the field,
    /// its value in the message, and the settings of this formatter.
    /// </summary>
    public static bool ShouldFormatFieldValue(IMessage message, FieldDescriptor field, object? value, bool formatDefaultValues) =>
        field.HasPresence
        // Fields that support presence *just* use that
        ? field.Accessor.HasValue(message)
        // Otherwise, format if either we've been asked to format default values, or if it's
        // not a default value anyway.
        : formatDefaultValues || !IsDefaultValue(field, value);

    private static bool IsDefaultValue(FieldDescriptor descriptor, object? value)
    {
        if (value == null)
        {
            return true;
        }
        if (descriptor.IsMap)
        {
            var dictionary = (IDictionary)value;
            return dictionary.Count == 0;
        }
        if (descriptor.IsRepeated)
        {
            var list = (IList)value;
            return list.Count == 0;
        }
        switch (descriptor.FieldType)
        {
            case FieldType.Bool:
                return (bool)value == false;
            case FieldType.Bytes:
                return (ByteString)value == ByteString.Empty;
            case FieldType.String:
                return (string)value == string.Empty;
            case FieldType.Double:
                return (double)value == 0.0;
            case FieldType.SInt32:
            case FieldType.Int32:
            case FieldType.SFixed32:
            case FieldType.Enum:
                return (int)value == 0;
            case FieldType.Fixed32:
            case FieldType.UInt32:
                return (uint)value == 0;
            case FieldType.Fixed64:
            case FieldType.UInt64:
                return (ulong)value == 0;
            case FieldType.SFixed64:
            case FieldType.Int64:
            case FieldType.SInt64:
                return (long)value == 0;
            case FieldType.Float:
                return (float)value == 0f;
            case FieldType.Message:
            case FieldType.Group: // Never expect to get this, but...
                return value == null;
            default:
                throw new ArgumentException("Invalid field type");
        }
    }
}
