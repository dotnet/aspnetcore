// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.AspNetCore.SignalR.Client.InMemory;

/// <summary>
/// An <see cref="EndPoint"/> that represents an in-memory connection to a hub.
/// </summary>
internal sealed class InMemoryEndPoint : EndPoint
{
    private readonly string _hubName;

    public InMemoryEndPoint(string hubName)
    {
        _hubName = hubName;
    }

    /// <inheritdoc />
    public override string ToString() => $"in-memory://{_hubName}";
}
