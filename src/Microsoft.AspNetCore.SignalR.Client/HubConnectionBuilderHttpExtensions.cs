// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionBuilderHttpExtensions
    {
        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url)
        {
            hubConnectionBuilder.WithUrl(new Uri(url), null, _ => { });
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url, Action<HttpConnectionOptions> configureHttpConnection)
        {
            hubConnectionBuilder.WithUrl(new Uri(url), null, configureHttpConnection);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url, HttpTransportType? transportType)
        {
            hubConnectionBuilder.WithUrl(new Uri(url), transportType, _ => { });
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url, HttpTransportType? transportType, Action<HttpConnectionOptions> configureHttpConnection)
        {
            hubConnectionBuilder.WithUrl(new Uri(url), transportType, configureHttpConnection);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url)
        {
            hubConnectionBuilder.WithUrl(url, null, _ => { });
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url, Action<HttpConnectionOptions> configureHttpConnection)
        {
            hubConnectionBuilder.WithUrl(url, null, configureHttpConnection);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url, HttpTransportType? transportType)
        {
            hubConnectionBuilder.WithUrl(url, null, _ => { });
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url, HttpTransportType? transportType, Action<HttpConnectionOptions> configureHttpConnection)
        {
            hubConnectionBuilder.Services.Configure<HttpConnectionOptions>(o =>
            {
                o.Url = url;
                o.Transport = transportType;
            });

            if (configureHttpConnection != null)
            {
                hubConnectionBuilder.Services.Configure(configureHttpConnection);
            }

            hubConnectionBuilder.Services.AddSingleton(services =>
            {
                var value = services.GetService<IOptions<HttpConnectionOptions>>().Value;

                var httpOptions = new HttpOptions
                {
                    HttpMessageHandlerFactory = value.MessageHandlerFactory,
                    Headers = value._headers != null ? new ReadOnlyDictionary<string, string>(value._headers) : null,
                    AccessTokenFactory = value.AccessTokenFactory,
                    WebSocketOptions = value.WebSocketOptions,
                    Cookies = value._cookies,
                    Proxy = value.Proxy,
                    UseDefaultCredentials = value.UseDefaultCredentials,
                    ClientCertificates = value._clientCertificates,
                    Credentials = value.Credentials,
                };

                Func<IConnection> createConnection = () => new HttpConnection(
                    value.Url,
                    value.Transport ?? HttpTransportType.All,
                    services.GetService<ILoggerFactory>(),
                    httpOptions);

                return createConnection;
            });

            return hubConnectionBuilder;
        }
    }
}
