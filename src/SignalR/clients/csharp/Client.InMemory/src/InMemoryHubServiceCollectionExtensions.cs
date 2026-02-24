// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Shared;
using Microsoft.AspNetCore.SignalR.Client.InMemory;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add in-memory hub connection support.
/// </summary>
public static class InMemoryHubServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services required to accept in-memory connections for the specified <typeparamref name="THub"/>.
    /// This should be called on the server-side <see cref="IServiceCollection"/> alongside
    /// <c>AddSignalR()</c>.
    /// </summary>
    /// <typeparam name="THub">The Hub type to accept in-memory connections for.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="connectionHandlerType">The closed generic <c>HubConnectionHandler&lt;THub&gt;</c> type.
    /// Typically: <c>typeof(HubConnectionHandler&lt;MyHub&gt;)</c>.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    ///
    /// builder.Services.AddSignalR();
    /// builder.Services.AddInMemoryHubConnection&lt;ChatHub&gt;(typeof(HubConnectionHandler&lt;ChatHub&gt;));
    /// </code>
    /// </example>
    public static IServiceCollection AddInMemoryHubConnection<THub>(
        this IServiceCollection services,
        Type connectionHandlerType) where THub : class
    {
        ArgumentNullThrowHelper.ThrowIfNull(services);
        ArgumentNullThrowHelper.ThrowIfNull(connectionHandlerType);

        services.AddSingleton(new InMemoryConnectionChannel<THub>(connectionHandlerType));
        services.AddHostedService<InMemoryHubConnectionDispatcher<THub>>();

        return services;
    }
}
