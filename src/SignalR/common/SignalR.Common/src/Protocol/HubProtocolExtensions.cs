// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// Extension methods for <see cref="IHubProtocol"/>.
/// </summary>
public static class HubProtocolExtensions
{
    /// <summary>
    /// Converts the specified <see cref="HubMessage"/> to its serialized representation.
    /// </summary>
    /// <param name="hubProtocol">The hub protocol.</param>
    /// <param name="message">The message to convert to bytes.</param>
    /// <returns>The serialized representation of the specified message.</returns>
    public static byte[] GetMessageBytes(this IHubProtocol hubProtocol, HubMessage message)
    {
        var writer = MemoryBufferWriter.Get();
        try
        {
            hubProtocol.WriteMessage(message, writer);
            return writer.ToArray();
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }
}
