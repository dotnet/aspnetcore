// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Shared;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class MessageTypeInfoResolver : IJsonTypeInfoResolver
{
    private readonly JsonContext _context;

    public MessageTypeInfoResolver(JsonContext context)
    {
        _context = context;
    }

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        if (!IsStandardMessage(type, out var messageDescriptor))
        {
            return null;
        }

        var typeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, options);
        typeInfo.CreateObject = () => Activator.CreateInstance(type)!;

        var fields = messageDescriptor.Fields.InFieldNumberOrder();
        var mappings = CreateJsonFieldMap(fields);

        foreach (var field in fields)
        {
            mappings.Remove(field.JsonName);

            var propertyInfo = CreatePropertyInfo(typeInfo, field.JsonName, field, isWritable: true);
            typeInfo.Properties.Add(propertyInfo);
        }

        // Fields have two mappings: the original field name and the camelcased JSON name.
        // The JSON name can also be customized in proto with json_name option.
        // Add extra setter only properties for mappings that haven't already been added.
        foreach (var mapping in mappings)
        {
            var propertyInfo = CreatePropertyInfo(typeInfo, mapping.Key, mapping.Value, isWritable: false);
            typeInfo.Properties.Add(propertyInfo);
        }

        return typeInfo;
    }

    private static bool IsStandardMessage(Type type, [NotNullWhen(true)] out MessageDescriptor? messageDescriptor)
    {
        if (!typeof(IMessage).IsAssignableFrom(type))
        {
            messageDescriptor = null;
            return false;
        }

        messageDescriptor = JsonConverterHelper.GetMessageDescriptor(type);
        if (messageDescriptor == null)
        {
            return false;
        }

        // Wrappers and well known types are handled by converters.
        if (ServiceDescriptorHelpers.IsWrapperType(messageDescriptor))
        {
            return false;
        }
        if (JsonConverterHelper.WellKnownTypeNames.ContainsKey(messageDescriptor.FullName))
        {
            return false;
        }

        return true;
    }

    private JsonPropertyInfo CreatePropertyInfo(JsonTypeInfo typeInfo, string name, FieldDescriptor field, bool isWritable)
    {
        var propertyInfo = typeInfo.CreateJsonPropertyInfo(
            JsonConverterHelper.GetFieldType(field),
            name);

        if (isWritable)
        {
            propertyInfo.ShouldSerialize = (o, v) =>
            {
                return JsonConverterHelper.ShouldFormatFieldValue((IMessage)o, field, v, !_context.Settings.IgnoreDefaultValues);
            };
            propertyInfo.Get = (o) =>
            {
                return field.Accessor.GetValue((IMessage)o);
            };
        }

        propertyInfo.Set = GetSetMethod(field);

        return propertyInfo;
    }

    private static Action<object, object?> GetSetMethod(FieldDescriptor field)
    {
        if (field.IsMap)
        {
            return (o, v) =>
            {
                // The serializer creates a collection. Copy contents to collection on read=only property.
                var existingValue = (IDictionary)field.Accessor.GetValue((IMessage)o);
                foreach (DictionaryEntry item in (IDictionary)v!)
                {
                    existingValue[item.Key] = item.Value;
                }
            };
        }

        if (field.IsRepeated)
        {
            return (o, v) =>
            {
                // The serializer creates a collection. Copy contents to collection on read=only property.
                var existingValue = (IList)field.Accessor.GetValue((IMessage)o);
                foreach (var item in (IList)v!)
                {
                    existingValue.Add(item);
                }
            };
        }

        if (field.RealContainingOneof != null)
        {
            return (o, v) =>
            {
                var caseField = field.RealContainingOneof.Accessor.GetCaseFieldDescriptor((IMessage)o);
                if (caseField != null)
                {
                    throw new InvalidOperationException($"Multiple values specified for oneof {field.RealContainingOneof.Name}.");
                }

                field.Accessor.SetValue((IMessage)o, v);
            };
        }

        return (o, v) =>
        {
            field.Accessor.SetValue((IMessage)o, v);
        };
    }

    private static Dictionary<string, FieldDescriptor> CreateJsonFieldMap(IList<FieldDescriptor> fields)
    {
        var map = new Dictionary<string, FieldDescriptor>();
        foreach (var field in fields)
        {
            map[field.Name] = field;
            map[field.JsonName] = field;
        }
        return new Dictionary<string, FieldDescriptor>(map);
    }
}
