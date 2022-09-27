// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Context for the hub lifetime events <see cref="Hub.OnConnectedAsync"/> and <see cref="Hub.OnDisconnectedAsync(Exception)"/>.
/// </summary>
public sealed class HubLifetimeContext
{
    /// <summary>
    /// Instantiates a new instance of the <see cref="HubLifetimeContext"/> class.
    /// </summary>
    /// <param name="context">Context for the active Hub connection and caller.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> specific to the scope of this Hub method invocation.</param>
    /// <param name="hub">The instance of the Hub.</param>
    public HubLifetimeContext(HubCallerContext context, IServiceProvider serviceProvider, Hub hub)
    {
        Hub = hub;
        ServiceProvider = serviceProvider;
        Context = context;
    }

    /// <summary>
    /// Gets the context for the active Hub connection and caller.
    /// </summary>
    public HubCallerContext Context { get; }

    /// <summary>
    /// Gets the Hub instance.
    /// </summary>
    public Hub Hub { get; }

    /// <summary>
    /// The <see cref="IServiceProvider"/> specific to the scope of this Hub method invocation.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
}
