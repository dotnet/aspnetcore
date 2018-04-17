// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR
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

        public SerializedHubMessage(IReadOnlyList<SerializedMessage> messages)
        {
            for (var i = 0; i < messages.Count; i++)
            {
                var message = messages[i];
                SetCache(message.ProtocolName, message.Serialized);
            }
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

        private void SetCache(string protocolName, ReadOnlyMemory<byte> serialized)
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

        private bool TryGetCached(string protocolName, out ReadOnlyMemory<byte> result)
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
    }
}
