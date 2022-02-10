// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// A builder abstraction for configuring <see cref="HubConnection"/> instances.
/// </summary>
public interface IHubConnectionBuilder : ISignalRBuilder
{
    /// <summary>
    /// Creates a <see cref="HubConnection"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="HubConnection"/> built using the configured options.
    /// </returns>
    HubConnection Build();
}
