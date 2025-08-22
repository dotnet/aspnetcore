// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Extension methods for <see cref="IConnectionBuilder"/>.
/// </summary>
public static class SignalRConnectionBuilderExtensions
{
    /// <summary>
    /// Configure the connection to host the specified <see cref="Hub"/> type.
    /// </summary>
    /// <typeparam name="THub">The <see cref="Hub"/> type to host on the connection.</typeparam>
    /// <param name="connectionBuilder">The connection to configure.</param>
    /// <returns>The same instance of the <see cref="IConnectionBuilder"/> for chaining.</returns>
    public static IConnectionBuilder UseHub<[DynamicallyAccessedMembers(Hub.DynamicallyAccessedMembers)] THub>(this IConnectionBuilder connectionBuilder) where THub : Hub
    {
        var marker = connectionBuilder.ApplicationServices.GetService(typeof(SignalRCoreMarkerService));
        if (marker == null)
        {
            throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling " +
                "'IServiceCollection.AddSignalR' inside the call to 'ConfigureServices(...)' in the application startup code.");
        }

        return connectionBuilder.UseConnectionHandler<HubConnectionHandler<THub>>();
    }
}
