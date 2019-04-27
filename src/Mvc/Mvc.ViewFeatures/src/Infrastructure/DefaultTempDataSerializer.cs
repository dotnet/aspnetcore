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
                switch (item.Value.Type)
                {
                    case JsonValueType.False:
                    case JsonValueType.True:
                        deserializedValue = item.Value.GetBoolean();
                        break;

                    case JsonValueType.Number:
                        deserializedValue = item.Value.GetInt32();
                        break;

                    case JsonValueType.String:
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

                    case JsonValueType.Null:
                        deserializedValue = null;
                        break;

                    default:
                        throw new InvalidOperationException(Resources.FormatTempData_CannotDeserializeType(item.Value.Type));
                }

                deserialized[item.Name] = deserializedValue;
            }

            return deserialized;
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
                type == typeof(Guid);
        }
    }
}
