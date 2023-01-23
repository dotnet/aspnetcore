// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Extension methods for <see cref="IHubConnectionBuilder"/>.
/// </summary>
public static class HubConnectionBuilderExtensions
{
    /// <summary>
    /// Adds a delegate for configuring the provided <see cref="ILoggingBuilder"/>. This may be called multiple times.
    /// </summary>
    /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
    /// <param name="configureLogging">The delegate that configures the <see cref="ILoggingBuilder"/>.</param>
    /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
    public static IHubConnectionBuilder ConfigureLogging(this IHubConnectionBuilder hubConnectionBuilder, Action<ILoggingBuilder> configureLogging)
    {
        hubConnectionBuilder.Services.AddLogging(configureLogging);
        return hubConnectionBuilder;
    }

    /// <summary>
    /// Configures the <see cref="HubConnection"/> to automatically attempt to reconnect if the connection is lost.
    /// The client will wait the default 0, 2, 10 and 30 seconds respectively before trying up to four reconnect attempts.
    /// </summary>
    /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
    /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
    public static IHubConnectionBuilder WithAutomaticReconnect(this IHubConnectionBuilder hubConnectionBuilder)
    {
        hubConnectionBuilder.Services.AddSingleton<IRetryPolicy>(new DefaultRetryPolicy());
        return hubConnectionBuilder;
    }

    /// <summary>
    /// Configures the <see cref="HubConnection"/> to automatically attempt to reconnect if the connection is lost.
    /// </summary>
    /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
    /// <param name="reconnectDelays">
    /// An array containing the delays before trying each reconnect attempt.
    /// The length of the array represents how many failed reconnect attempts it takes before the client will stop attempting to reconnect.
    /// </param>
    /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
    public static IHubConnectionBuilder WithAutomaticReconnect(this IHubConnectionBuilder hubConnectionBuilder, TimeSpan[] reconnectDelays)
    {
        hubConnectionBuilder.Services.AddSingleton<IRetryPolicy>(new DefaultRetryPolicy(reconnectDelays));
        return hubConnectionBuilder;
    }

    /// <summary>
    /// Configures the <see cref="HubConnection"/> to automatically attempt to reconnect if the connection is lost.
    /// </summary>
    /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
    /// <param name="retryPolicy">An <see cref="IRetryPolicy"/> that controls the timing and number of reconnect attempts.</param>
    /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
    public static IHubConnectionBuilder WithAutomaticReconnect(this IHubConnectionBuilder hubConnectionBuilder, IRetryPolicy retryPolicy)
    {
        hubConnectionBuilder.Services.AddSingleton(retryPolicy);
        return hubConnectionBuilder;
    }

    /// <summary>
    /// Configures ServerTimeout for the <see cref="HubConnection" />.
    /// </summary>
    /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
    /// <param name="timeout">ServerTimeout for the <see cref="HubConnection"/>.</param>
    /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
    public static IHubConnectionBuilder WithServerTimeout(this IHubConnectionBuilder hubConnectionBuilder, TimeSpan timeout)
    {
        hubConnectionBuilder.Services.Configure<HubConnectionOptions>(o => o.ServerTimeout = timeout);
        return hubConnectionBuilder;
    }

    /// <summary>
    /// Configures KeepAliveInterval for the <see cref="HubConnection" />.
    /// </summary>
    /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
    /// <param name="interval">KeepAliveInterval for the <see cref="HubConnection"/>.</param>
    /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
    public static IHubConnectionBuilder WithKeepAliveInterval(this IHubConnectionBuilder hubConnectionBuilder, TimeSpan interval)
    {
        hubConnectionBuilder.Services.Configure<HubConnectionOptions>(o => o.KeepAliveInterval = interval);
        return hubConnectionBuilder;
    }
}
