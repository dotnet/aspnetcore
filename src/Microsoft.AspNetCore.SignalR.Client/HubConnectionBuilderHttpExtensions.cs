// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionBuilderHttpExtensions
    {
        public static readonly string TransportTypeKey = "TransportType";
        public static readonly string HttpMessageHandlerKey = "HttpMessageHandler";

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, string url)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            return hubConnectionBuilder.WithUrl(new Uri(url));
        }

        public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder hubConnectionBuilder, Uri url)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            hubConnectionBuilder.ConfigureConnectionFactory(() =>
            {
                return new HttpConnection(url,
                    hubConnectionBuilder.GetTransport(),
                    hubConnectionBuilder.GetLoggerFactory(),
                    hubConnectionBuilder.GetMessageHandler());
            });
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithTransport(this IHubConnectionBuilder hubConnectionBuilder, TransportType transportType)
        {
            hubConnectionBuilder.AddSetting(TransportTypeKey, transportType);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithMessageHandler(this IHubConnectionBuilder hubConnectionBuilder, HttpMessageHandler httpMessageHandler)
        {
            hubConnectionBuilder.AddSetting(HttpMessageHandlerKey, httpMessageHandler);
            return hubConnectionBuilder;
        }

        public static TransportType GetTransport(this IHubConnectionBuilder hubConnectionBuilder)
        {
            if (hubConnectionBuilder.TryGetSetting<TransportType>(TransportTypeKey, out var transportType))
            {
                return transportType;
            }

            return TransportType.All;
        }

        public static HttpMessageHandler GetMessageHandler(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<HttpMessageHandler>(HttpMessageHandlerKey, out var messageHandler);
            return messageHandler;
        }
    }
}
