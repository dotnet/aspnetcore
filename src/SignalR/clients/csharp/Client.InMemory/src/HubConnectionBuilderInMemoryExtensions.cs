// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Shared;
using Microsoft.AspNetCore.SignalR.Client.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Extension methods for <see cref="IHubConnectionBuilder"/> to configure in-memory hub connections.
/// </summary>
public static class HubConnectionBuilderInMemoryExtensions
{
    /// <summary>
    /// Configures the <see cref="HubConnection"/> to use an in-memory transport that connects directly to
    /// a <typeparamref name="THub"/> in the same process, bypassing HTTP entirely.
    /// </summary>
    /// <typeparam name="THub">The Hub type to connect to. The server must have called
    /// <see cref="InMemoryHubServiceCollectionExtensions.AddInMemoryHubConnection{THub}(IServiceCollection, Type)"/>
    /// for the same hub type.</typeparam>
    /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
    /// <param name="serverServices">The server-side <see cref="IServiceProvider"/> that contains the
    /// <see cref="InMemoryConnectionChannel{THub}"/>. In Blazor Server, this is typically
    /// the <c>Services</c> property on the component.</param>
    /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// var connection = new HubConnectionBuilder()
    ///     .WithInMemoryHub&lt;ChatHub&gt;(Services)
    ///     .Build();
    ///
    /// await connection.StartAsync();
    /// </code>
    /// </example>
    public static IHubConnectionBuilder WithInMemoryHub<THub>(
        this IHubConnectionBuilder hubConnectionBuilder,
        IServiceProvider serverServices) where THub : class
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnectionBuilder);
        ArgumentNullThrowHelper.ThrowIfNull(serverServices);

        var channel = serverServices.GetRequiredService<InMemoryConnectionChannel<THub>>();

        hubConnectionBuilder.Services.AddSingleton(channel);
        hubConnectionBuilder.Services.AddSingleton<IConnectionFactory, InMemoryConnectionFactory<THub>>();
        hubConnectionBuilder.Services.AddSingleton<System.Net.EndPoint>(new InMemoryEndPoint(typeof(THub).Name));

        return hubConnectionBuilder;
    }
}
