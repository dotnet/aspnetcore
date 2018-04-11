// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionBuilderHttpExtensions
    {
        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url)
        {
            hubConnectionBuilder.WithUrlCore(new Uri(url), null, _ => { });
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url, Action<HttpConnectionOptions> configureHttpConnection)
        {
            hubConnectionBuilder.WithUrlCore(new Uri(url), null, configureHttpConnection);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url, HttpTransportType transports)
        {
            hubConnectionBuilder.WithUrlCore(new Uri(url), transports, _ => { });
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url, HttpTransportType transports, Action<HttpConnectionOptions> configureHttpConnection)
        {
            hubConnectionBuilder.WithUrlCore(new Uri(url), transports, configureHttpConnection);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url)
        {
            hubConnectionBuilder.WithUrlCore(url, null, _ => { });
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url, Action<HttpConnectionOptions> configureHttpConnection)
        {
            hubConnectionBuilder.WithUrlCore(url, null, configureHttpConnection);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url, HttpTransportType transports)
        {
            hubConnectionBuilder.WithUrlCore(url, null, _ => { });
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url, HttpTransportType transports, Action<HttpConnectionOptions> configureHttpConnection)
        {
            hubConnectionBuilder.WithUrlCore(url, transports, _ => { });
            return hubConnectionBuilder;
        }

        private static IHubConnectionBuilder WithUrlCore(this IHubConnectionBuilder hubConnectionBuilder, Uri url, HttpTransportType? transports, Action<HttpConnectionOptions> configureHttpConnection)
        {
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
