// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    /// <summary>
    /// This class is designed to support the framework. The API is subject to breaking changes.
    /// Represents a serialization cache for a single message.
    /// </summary>
    public class SerializedHubMessage
    {
        private SerializedMessage _cachedItem1;
        private SerializedMessage _cachedItem2;
        private IList<SerializedMessage> _cachedItems;

        public HubMessage Message { get; }

        private SerializedHubMessage()
        {
        }

        public SerializedHubMessage(HubMessage message)
        {
            Message = message;
        }

        public ReadOnlyMemory<byte> GetSerializedMessage(IHubProtocol protocol)
        {
            if (!TryGetCached(protocol.Name, out var serialized))
            {
                if (Message == null)
                {
                    throw new InvalidOperationException(
                        "This message was received from another server that did not have the requested protocol available.");
                }

                serialized = protocol.GetMessageBytes(Message);
                SetCache(protocol.Name, serialized);
            }

            return serialized;
        }

        public static void WriteAllSerializedVersions(BinaryWriter writer, HubMessage message, IReadOnlyList<IHubProtocol> protocols)
        {
            // The serialization format is based on BinaryWriter
            // * 1 byte number of protocols
            // * For each protocol:
            //   * Length-prefixed string using 7-bit variable length encoding (length depends on BinaryWriter's encoding)
            //   * 4 byte length of the buffer
            //   * N byte buffer

            if (protocols.Count > byte.MaxValue)
            {
                throw new InvalidOperationException($"Can't serialize cache containing more than {byte.MaxValue} entries");
            }

            writer.Write((byte)protocols.Count);
            foreach (var protocol in protocols)
            {
                writer.Write(protocol.Name);

                var buffer = protocol.GetMessageBytes(message);
                writer.Write(buffer.Length);
                writer.Write(buffer);
            }
        }

        public static SerializedHubMessage ReadAllSerializedVersions(BinaryReader reader)
        {
            var cache = new SerializedHubMessage();
            var count = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var protocol = reader.ReadString();
                var length = reader.ReadInt32();
                var serialized = reader.ReadBytes(length);
                cache.SetCache(protocol, serialized);
            }

            return cache;
        }

        private void SetCache(string protocolName, byte[] serialized)
        {
            if (_cachedItem1.ProtocolName == null)
            {
                _cachedItem1 = new SerializedMessage(protocolName, serialized);
            }
            else if (_cachedItem2.ProtocolName == null)
            {
                _cachedItem2 = new SerializedMessage(protocolName, serialized);
            }
            else
            {
                if (_cachedItems == null)
                {
                    _cachedItems = new List<SerializedMessage>();
                }

                foreach (var item in _cachedItems)
                {
                    if (string.Equals(item.ProtocolName, protocolName, StringComparison.Ordinal))
                    {
                        // No need to add
                        return;
                    }
                }

                _cachedItems.Add(new SerializedMessage(protocolName, serialized));
            }
        }

        private bool TryGetCached(string protocolName, out byte[] result)
        {
            if (string.Equals(_cachedItem1.ProtocolName, protocolName, StringComparison.Ordinal))
            {
                result = _cachedItem1.Serialized;
                return true;
            }

            if (string.Equals(_cachedItem2.ProtocolName, protocolName, StringComparison.Ordinal))
            {
                result = _cachedItem2.Serialized;
                return true;
            }

            if (_cachedItems != null)
            {
                foreach (var serializedMessage in _cachedItems)
                {
                    if (string.Equals(serializedMessage.ProtocolName, protocolName, StringComparison.Ordinal))
                    {
                        result = serializedMessage.Serialized;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        private readonly struct SerializedMessage
        {
            public string ProtocolName { get; }
            public byte[] Serialized { get; }

            public SerializedMessage(string protocolName, byte[] serialized)
            {
                ProtocolName = protocolName;
                Serialized = serialized;
            }
        }
    }
}
