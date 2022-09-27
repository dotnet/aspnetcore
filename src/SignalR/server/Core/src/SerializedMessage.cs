// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Represents a serialized message.
/// </summary>
public readonly struct SerializedMessage
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
