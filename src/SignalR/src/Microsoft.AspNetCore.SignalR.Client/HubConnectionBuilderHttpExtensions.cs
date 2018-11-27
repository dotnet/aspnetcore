// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Client
{
    /// <summary>
    /// Extension methods for <see cref="IHubConnectionBuilder"/>.
    /// </summary>
    public static class HubConnectionBuilderHttpExtensions
    {
        /// <summary>
        /// Configures the <see cref="HubConnection" /> to use HTTP-based transports to connect to the specified URL.
        /// </summary>
        /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
        /// <param name="url">The URL the <see cref="HttpConnection"/> will use.</param>
        /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url)
        {
            hubConnectionBuilder.WithUrlCore(new Uri(url), null, null);
            return hubConnectionBuilder;
        }

        /// <summary>
        /// Configures the <see cref="HubConnection" /> to use HTTP-based transports to connect to the specified URL.
        /// </summary>
        /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
        /// <param name="url">The URL the <see cref="HttpConnection"/> will use.</param>
        /// <param name="configureHttpConnection">The delegate that configures the <see cref="HttpConnection"/>.</param>
        /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url, Action<HttpConnectionOptions> configureHttpConnection)
        {
            hubConnectionBuilder.WithUrlCore(new Uri(url), null, configureHttpConnection);
            return hubConnectionBuilder;
        }

        /// <summary>
        /// Configures the <see cref="HubConnection" /> to use HTTP-based transports to connect to the specified URL and transports.
        /// </summary>
        /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
        /// <param name="url">The URL the <see cref="HttpConnection"/> will use.</param>
        /// <param name="transports">A bitmask comprised of one or more <see cref="HttpTransportType"/> that specify what transports the client should use.</param>
        /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url, HttpTransportType transports)
        {
            hubConnectionBuilder.WithUrlCore(new Uri(url), transports, null);
            return hubConnectionBuilder;
        }

        /// <summary>
        /// Configures the <see cref="HubConnection" /> to use HTTP-based transports to connect to the specified URL and transports.
        /// </summary>
        /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
        /// <param name="url">The URL the <see cref="HttpConnection"/> will use.</param>
        /// <param name="transports">A bitmask comprised of one or more <see cref="HttpTransportType"/> that specify what transports the client should use.</param>
        /// <param name="configureHttpConnection">The delegate that configures the <see cref="HttpConnection"/>.</param>
        /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url, HttpTransportType transports, Action<HttpConnectionOptions> configureHttpConnection)
        {
            hubConnectionBuilder.WithUrlCore(new Uri(url), transports, configureHttpConnection);
            return hubConnectionBuilder;
        }

        /// <summary>
        /// Configures the <see cref="HubConnection" /> to use HTTP-based transports to connect to the specified URL.
        /// </summary>
        /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
        /// <param name="url">The URL the <see cref="HttpConnection"/> will use.</param>
        /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url)
        {
            hubConnectionBuilder.WithUrlCore(url, null, null);
            return hubConnectionBuilder;
        }

        /// <summary>
        /// Configures the <see cref="HubConnection" /> to use HTTP-based transports to connect to the specified URL.
        /// </summary>
        /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
        /// <param name="url">The URL the <see cref="HttpConnection"/> will use.</param>
        /// <param name="configureHttpConnection">The delegate that configures the <see cref="HttpConnection"/>.</param>
        /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url, Action<HttpConnectionOptions> configureHttpConnection)
        {
            hubConnectionBuilder.WithUrlCore(url, null, configureHttpConnection);
            return hubConnectionBuilder;
        }

        /// <summary>
        /// Configures the <see cref="HubConnection" /> to use HTTP-based transports to connect to the specified URL and transports.
        /// </summary>
        /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
        /// <param name="url">The URL the <see cref="HttpConnection"/> will use.</param>
        /// <param name="transports">A bitmask comprised of one or more <see cref="HttpTransportType"/> that specify what transports the client should use.</param>
        /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url, HttpTransportType transports)
        {
            hubConnectionBuilder.WithUrlCore(url, transports, null);
            return hubConnectionBuilder;
        }

        /// <summary>
        /// Configures the <see cref="HubConnection" /> to use HTTP-based transports to connect to the specified URL and transports.
        /// </summary>
        /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
        /// <param name="url">The URL the <see cref="HttpConnection"/> will use.</param>
        /// <param name="transports">A bitmask comprised of one or more <see cref="HttpTransportType"/> that specify what transports the client should use.</param>
        /// <param name="configureHttpConnection">The delegate that configures the <see cref="HttpConnection"/>.</param>
        /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url, HttpTransportType transports, Action<HttpConnectionOptions> configureHttpConnection)
        {
            hubConnectionBuilder.WithUrlCore(url, transports, configureHttpConnection);
            return hubConnectionBuilder;
        }

        private static IHubConnectionBuilder WithUrlCore(this IHubConnectionBuilder hubConnectionBuilder, Uri url, HttpTransportType? transports, Action<HttpConnectionOptions> configureHttpConnection)
        {
            if (hubConnectionBuilder == null)
            {
                throw new ArgumentNullException(nameof(hubConnectionBuilder));
            }

            hubConnectionBuilder.Services.Configure<HttpConnectionOptions>(o =>
            {
                o.Url = url;
                if (transports != null)
                {
                    o.Transports = transports.Value;
                }
            });

            if (configureHttpConnection != null)
            {
                hubConnectionBuilder.Services.Configure(configureHttpConnection);
            }

            hubConnectionBuilder.Services.AddSingleton<IConnectionFactory, HttpConnectionFactory>();
            return hubConnectionBuilder;
        }
    }
}
