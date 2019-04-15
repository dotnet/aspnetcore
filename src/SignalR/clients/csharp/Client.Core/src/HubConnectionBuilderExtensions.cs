// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client
{
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
            // REVIEW: Should we throw if this would override another IRetryPolicy?
            // REVIEW: If we have a separate retry policy for retrying on start failures vs retrying only after
            // a connection was fully established, would we create an IStartRetryPolicyHolder with a single IRetryPolicy
            // property so they could independently retrieved from the IoC container?

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
    }
}
