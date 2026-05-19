// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal;

internal readonly struct RedisInvocation
{
    /// <summary>
    /// Gets a list of connections that should be excluded from this invocation.
    /// May be null to indicate that no connections are to be excluded.
    /// </summary>
    public IReadOnlyList<string>? ExcludedConnectionIds { get; }

    /// <summary>
    /// Gets the message serialization cache containing serialized payloads for the message.
    /// </summary>
    public SerializedHubMessage Message { get; }

    public string? ReturnChannel { get; }

    public string? InvocationId { get; }

    public RedisInvocation(SerializedHubMessage message, IReadOnlyList<string>? excludedConnectionIds,
        string? invocationId = null, string? returnChannel = null)
    {
        Message = message;
        ExcludedConnectionIds = excludedConnectionIds;
        ReturnChannel = returnChannel;
        InvocationId = invocationId;
    }
}
