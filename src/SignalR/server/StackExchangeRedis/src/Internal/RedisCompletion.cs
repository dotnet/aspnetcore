// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal;

internal readonly struct RedisCompletion
{
    public ReadOnlySequence<byte> CompletionMessage { get; }

    public string ProtocolName { get; }

    public RedisCompletion(string protocolName, ReadOnlySequence<byte> completionMessage)
    {
        ProtocolName = protocolName;
        CompletionMessage = completionMessage;
    }
}
