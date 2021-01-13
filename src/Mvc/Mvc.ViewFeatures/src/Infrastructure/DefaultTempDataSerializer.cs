// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure
{
    internal class DefaultTempDataSerializer : TempDataSerializer
    {
        public override IDictionary<string, object> Deserialize(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length == 0)
            {
                return new Dictionary<string, object>();
            }

            using var jsonDocument = JsonDocument.Parse(value);
            var rootElement = jsonDocument.RootElement;
            return DeserializeDictionary(rootElement);
        }

        private IDictionary<string, object> DeserializeDictionary(JsonElement rootElement)
        {
            var deserialized = new Dictionary<string, object>(StringComparer.Ordinal);

            foreach (var item in rootElement.EnumerateObject())
            {
                object deserializedValue;
                switch (item.Value.ValueKind)
                {
                    case JsonValueKind.False:
                    case JsonValueKind.True:
                        deserializedValue = item.Value.GetBoolean();
                        break;

                    case JsonValueKind.Number:
                        deserializedValue = item.Value.GetInt32();
                        break;

                    case JsonValueKind.String:
                        if (item.Value.TryGetGuid(out var guid))
                        {
                            deserializedValue = guid;
                        }
                        else if (item.Value.TryGetDateTime(out var dateTime))
                        {
                            deserializedValue = dateTime;
                        }
                        else
                        {
                            deserializedValue = item.Value.GetString();
                        }
                        break;

                    case JsonValueKind.Null:
                        deserializedValue = null;
                        break;

                    case JsonValueKind.Array:
                        deserializedValue = DeserializeArray(item.Value);
                        break;

                    case JsonValueKind.Object:
                        deserializedValue = DeserializeDictionaryEntry(item.Value);
                        break;

                    default:
                        throw new InvalidOperationException(Resources.FormatTempData_CannotDeserializeType(item.Value.ValueKind));
                }

                deserialized[item.Name] = deserializedValue;
            }

            return deserialized;
        }

        private static object DeserializeArray(in JsonElement arrayElement)
        {
            if (arrayElement.GetArrayLength() == 0)
            {
                // We have to infer the type of the array by inspecting it's elements.
                // If there's nothing to inspect, return a null value since we do not know
                // what type the user code is expecting.
                return null;
            }

            if (arrayElement[0].ValueKind == JsonValueKind.String)
            {
                var array = new List<string>();

                foreach (var item in arrayElement.EnumerateArray())
                {
                    array.Add(item.GetString());
                }

                return array.ToArray();
            }
            else if (arrayElement[0].ValueKind == JsonValueKind.Number)
            {
                var array = new List<int>();

                foreach (var item in arrayElement.EnumerateArray())
                {
                    array.Add(item.GetInt32());
                }

                return array.ToArray();
            }

            throw new InvalidOperationException(Resources.FormatTempData_CannotDeserializeType(arrayElement.ValueKind));
        }

        private static object DeserializeDictionaryEntry(in JsonElement objectElement)
        {
            var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in objectElement.EnumerateObject())
            {
                dictionary[item.Name] = item.Value.GetString();
            }

            return dictionary;
        }

        public override byte[] Serialize(IDictionary<string, object> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<byte>();
            }

            using (var bufferWriter = new ArrayBufferWriter<byte>())
            {
                using var writer = new Utf8JsonWriter(bufferWriter);
                writer.WriteStartObject();
                foreach (var (key, value) in values)
                {
                    if (value == null)
                    {
                        writer.WriteNull(key);
                        continue;
                    }

                    // We want to allow only simple types to be serialized.
                    if (!CanSerializeType(value.GetType()))
                    {
                        throw new InvalidOperationException(
                            Resources.FormatTempData_CannotSerializeType(
                                typeof(DefaultTempDataSerializer).FullName,
                                value.GetType()));
                    }

                    switch (value)
                    {
                        case Enum _:
                            writer.WriteNumber(key, (int)value);
                            break;

                        case string stringValue:
                            writer.WriteString(key, stringValue);
                            break;

                        case int intValue:
                            writer.WriteNumber(key, intValue);
                            break;

                        case bool boolValue:
                            writer.WriteBoolean(key, boolValue);
                            break;

                        case DateTime dateTime:
                            writer.WriteString(key, dateTime);
                            break;

                        case Guid guid:
                            writer.WriteString(key, guid);
                            break;

                        case ICollection<int> intCollection:
                            writer.WriteStartArray(key);
                            foreach (var element in intCollection)
                            {
                                writer.WriteNumberValue(element);
                            }
                            writer.WriteEndArray();
                            break;

                        case ICollection<string> stringCollection:
                            writer.WriteStartArray(key);
                            foreach (var element in stringCollection)
                            {
                                writer.WriteStringValue(element);
                            }
                            writer.WriteEndArray();
                            break;

                        case IDictionary<string, string> dictionary:
                            writer.WriteStartObject(key);
                            foreach (var element in dictionary)
                            {
                                writer.WriteString(element.Key, element.Value);
                            }
                            writer.WriteEndObject();
                            break;
                    }
                }
                writer.WriteEndObject();
                writer.Flush();

                return bufferWriter.WrittenMemory.ToArray();
            }
        }

        public override bool CanSerializeType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            type = Nullable.GetUnderlyingType(type) ?? type;

            return
                type.IsEnum ||
                type == typeof(int) ||
                type == typeof(string) ||
                type == typeof(bool) ||
                type == typeof(DateTime) ||
                type == typeof(Guid) ||
                typeof(ICollection<int>).IsAssignableFrom(type) ||
                typeof(ICollection<string>).IsAssignableFrom(type) ||
                typeof(IDictionary<string, string>).IsAssignableFrom(type);
        }
    }
}
