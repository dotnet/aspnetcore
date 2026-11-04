// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal;

/// <summary>
/// Represents a serialization cache for a single message.
/// </summary>
internal class SerializedHubMessage
{
    private SerializedMessage _cachedItem1;
    private SerializedMessage _cachedItem2;
    private List<SerializedMessage>? _cachedItems;
    private readonly object _lock = new object();

    /// <summary>
    /// Gets the hub message for the serialization cache.
    /// </summary>
    public HubMessage? Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedHubMessage"/> class.
    /// </summary>
    /// <param name="messages">A collection of already serialized messages to cache.</param>
    public SerializedHubMessage(IReadOnlyList<SerializedMessage> messages)
    {
        // A lock isn't needed here because nobody has access to this type until the constructor finishes.
        for (var i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            SetCacheUnsynchronized(message.ProtocolName, message.Serialized);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedHubMessage"/> class.
    /// </summary>
    /// <param name="message">The hub message for the cache. This will be serialized with an <see cref="IHubProtocol"/> in <see cref="GetSerializedMessage"/> to get the message's serialized representation.</param>
    public SerializedHubMessage(HubMessage message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the serialized representation of the <see cref="HubMessage"/> using the specified <see cref="IHubProtocol"/>.
    /// </summary>
    /// <param name="protocol">The protocol used to create the serialized representation.</param>
    /// <returns>The serialized representation of the <see cref="HubMessage"/>.</returns>
    public ReadOnlyMemory<byte> GetSerializedMessage(IHubProtocol protocol)
    {
        lock (_lock)
        {
            if (!TryGetCachedUnsynchronized(protocol.Name, out var serialized))
            {
                if (Message == null)
                {
                    throw new InvalidOperationException(
                        "This message was received from another server that did not have the requested protocol available.");
                }

                serialized = protocol.GetMessageBytes(Message);
                SetCacheUnsynchronized(protocol.Name, serialized);
            }

            return serialized;
        }
    }

    // Used for unit testing.
    internal IReadOnlyList<SerializedMessage> GetAllSerializations()
    {
        // Even if this is only used in tests, let's do it right.
        lock (_lock)
        {
            if (_cachedItem1.ProtocolName == null)
            {
                return Array.Empty<SerializedMessage>();
            }

            var list = new List<SerializedMessage>(2);
            list.Add(_cachedItem1);

            if (_cachedItem2.ProtocolName != null)
            {
                list.Add(_cachedItem2);

                if (_cachedItems != null)
                {
                    list.AddRange(_cachedItems);
                }
            }

            return list;
        }
    }

    private void SetCacheUnsynchronized(string protocolName, ReadOnlyMemory<byte> serialized)
    {
        // We set the fields before moving on to the list, if we need it to hold more than 2 items.
        // We have to read/write these fields under the lock because the structs might tear and another
        // thread might observe them half-assigned

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

    private bool TryGetCachedUnsynchronized(string protocolName, out ReadOnlyMemory<byte> result)
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

internal readonly struct SerializedMessage
{
    /// <summary>
    /// Gets the protocol of the serialized message.
    /// </summary>
    public string ProtocolName { get; }

    /// <summary>
    /// Gets the serialized representation of the message.
    /// </summary>
    public ReadOnlyMemory<byte> Serialized { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedMessage"/> class.
    /// </summary>
    /// <param name="protocolName">The protocol of the serialized message.</param>
    /// <param name="serialized">The serialized representation of the message.</param>
    public SerializedMessage(string protocolName, ReadOnlyMemory<byte> serialized)
    {
        ProtocolName = protocolName;
        Serialized = serialized;
    }
}
