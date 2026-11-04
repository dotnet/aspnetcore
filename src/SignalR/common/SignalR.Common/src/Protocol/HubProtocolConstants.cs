// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// Constants related to the SignalR hub protocol.
/// </summary>
public static class HubProtocolConstants
{
    /// <summary>
    /// Represents the invocation message type.
    /// </summary>
    public const int InvocationMessageType = 1;

    /// <summary>
    /// Represents the stream item message type.
    /// </summary>
    public const int StreamItemMessageType = 2;

    /// <summary>
    /// Represents the completion message type.
    /// </summary>
    public const int CompletionMessageType = 3;

    /// <summary>
    /// Represents the stream invocation message type.
    /// </summary>
    public const int StreamInvocationMessageType = 4;

    /// <summary>
    /// Represents the cancel invocation message type.
    /// </summary>
    public const int CancelInvocationMessageType = 5;

    /// <summary>
    /// Represents the ping message type.
    /// </summary>
    public const int PingMessageType = 6;

    /// <summary>
    /// Represents the close message type.
    /// </summary>
    public const int CloseMessageType = 7;

    /// <summary>
    /// Represents the ack message type.
    /// </summary>
    public const int AckMessageType = 8;

    /// <summary>
    /// Represents the sequence message type.
    /// </summary>
    public const int SequenceMessageType = 9;
}
