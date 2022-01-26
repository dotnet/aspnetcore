// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal;

internal readonly struct RedisReturnResult
{
    /// <summary>
    /// Gets the message serialization cache containing serialized payloads for the message.
    /// </summary>
    public object? Result { get; }

    public string InvocationId { get; }

    public RedisReturnResult(string invocationId, object? result)
    {
        InvocationId = invocationId;
        Result = result;
    }
}

internal readonly struct RedisCompletion
{
    /// <summary>
    /// Gets the message serialization cache containing serialized payloads for the message.
    /// </summary>
    public ReadOnlySequence<byte> CompletionMessage { get; }

    public string ProtocolName { get; }

    public RedisCompletion(string protocolName, ReadOnlySequence<byte> completionMessage)
    {
        ProtocolName = protocolName;
        CompletionMessage = completionMessage;
    }
}
