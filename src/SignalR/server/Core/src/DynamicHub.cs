// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// A base class for SignalR hubs that use <c>dynamic</c> to represent client invocations.
/// </summary>
public abstract class DynamicHub : Hub
{
    private DynamicHubClients? _clients;

    /// <summary>
    /// Gets or sets an object that can be used to invoke methods on the clients connected to this hub.
    /// </summary>
    public new DynamicHubClients Clients
    {
        get
        {
            if (_clients == null)
            {
                _clients = new DynamicHubClients(base.Clients);
            }

            return _clients;
        }
        set => _clients = value;
    }
}
