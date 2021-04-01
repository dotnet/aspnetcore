// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using MessagePack;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.Components.Server.BlazorPack
{
    internal sealed class BlazorPackHubProtocolWorker : MessagePackHubProtocolWorker
    {
        protected override object DeserializeObject(ref MessagePackReader reader, Type type, string field)
        {
            try
            {
                if (type == typeof(string))
                {
                    return ReadString(ref reader, "argument");
                }
                else if (type == typeof(bool))
                {
                    return reader.ReadBoolean();
                }
                else if (type == typeof(int))
                {
                    return reader.ReadInt32();
                }
                else if (type == typeof(long))
                {
                    return reader.ReadInt64();
                }
                else if (type == typeof(float))
                {
                    return reader.ReadSingle();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Deserializing object of the `{type.Name}` type for '{field}' failed.", ex);
            }

            throw new FormatException($"Type {type} is not supported");
        }

        protected override void Serialize(ref MessagePackWriter writer, Type type, object value)
        {
            switch (value)
            {
                case null:
                    writer.WriteNil();
                    break;

                case bool boolValue:
                    writer.Write(boolValue);
                    break;

                case string stringValue:
                    writer.Write(stringValue);
                    break;

                case int intValue:
                    writer.Write(intValue);
                    break;

                case long longValue:
                    writer.Write(longValue);
                    break;

                case float floatValue:
                    writer.Write(floatValue);
                    break;

                case ArraySegment<byte> bytes:
                    writer.Write(bytes);
                    break;

                default:
                    throw new FormatException($"Unsupported argument type {type}");
            }
        }
    }
}
