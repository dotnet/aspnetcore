// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR;

internal class PooledSerializedMessage : IDisposable
{
    private (string ProtocolName, MemoryBufferWriter Writer) _cachedItem1;
    private (string ProtocolName, MemoryBufferWriter Writer) _cachedItem2;
    private List<(string ProtocolName, MemoryBufferWriter Writer)>? _cachedItems;

    private readonly object _lock = new object();

    public PooledSerializedMessage(HubMessage hubMessage)
    {
        Message = hubMessage;
    }

    public HubMessage Message { get; }

    /// <summary>
    /// Gets the serialized representation of the <see cref="HubMessage"/> using the specified <see cref="IHubProtocol"/>.
    /// </summary>
    /// <param name="protocol">The protocol used to create the serialized representation.</param>
    /// <returns>The serialized representation of the <see cref="HubMessage"/>.</returns>
    public MemoryBufferWriter GetSerializedMessage(IHubProtocol protocol)
    {
        lock (_lock)
        {
            if (!TryGetCachedUnsynchronized(protocol.Name, out var writer))
            {
                if (Message == null)
                {
                    throw new InvalidOperationException(
                        "This message was received from another server that did not have the requested protocol available.");
                }

                writer = new MemoryBufferWriter();

                protocol.WriteMessage(Message, writer);

                SetCacheUnsynchronized(protocol.Name, writer);
            }

            return writer;
        }
    }

    private void SetCacheUnsynchronized(string protocolName, MemoryBufferWriter writer)
    {
        // We set the fields before moving on to the list, if we need it to hold more than 2 items.
        // We have to read/write these fields under the lock because the structs might tear and another
        // thread might observe them half-assigned

        if (_cachedItem1.ProtocolName == null)
        {
            _cachedItem1 = (protocolName, writer);
        }
        else if (_cachedItem2.ProtocolName == null)
        {
            _cachedItem2 = (protocolName, writer);
        }
        else
        {
            if (_cachedItems == null)
            {
                _cachedItems = new List<(string, MemoryBufferWriter)>();
            }

            foreach (var item in _cachedItems)
            {
                if (string.Equals(item.ProtocolName, protocolName, StringComparison.Ordinal))
                {
                    // No need to add
                    return;
                }
            }

            _cachedItems.Add((protocolName, writer));
        }
    }

    private bool TryGetCachedUnsynchronized(string protocolName, [MaybeNullWhen(false)] out MemoryBufferWriter result)
    {
        if (string.Equals(_cachedItem1.ProtocolName, protocolName, StringComparison.Ordinal))
        {
            result = _cachedItem1.Writer;
            return true;
        }

        if (string.Equals(_cachedItem2.ProtocolName, protocolName, StringComparison.Ordinal))
        {
            result = _cachedItem2.Writer;
            return true;
        }

        if (_cachedItems != null)
        {
            foreach (var serializedMessage in _cachedItems)
            {
                if (string.Equals(serializedMessage.ProtocolName, protocolName, StringComparison.Ordinal))
                {
                    result = serializedMessage.Writer;
                    return true;
                }
            }
        }

        result = null;
        return false;
    }


    public void Dispose()
    {
        _cachedItem1.Writer?.Dispose();

        _cachedItem2.Writer?.Dispose();

        if (_cachedItems is not null)
        {
            foreach (var item in _cachedItems)
            {
                item.Writer.Dispose();
            }
        }
    }
}
