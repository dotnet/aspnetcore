// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Client.Http;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionBuilderHttpExtensions
    {
        public static readonly string TransportTypeKey = "TransportType";
        public static readonly string HttpMessageHandlerKey = "HttpMessageHandler";
        public static readonly string HeadersKey = "Headers";
        public static readonly string JwtBearerTokenFactoryKey = "JwtBearerTokenFactory";

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
                var headers = hubConnectionBuilder.GetHeaders();
                var httpOptions = new HttpOptions
                {
                    HttpMessageHandler = hubConnectionBuilder.GetMessageHandler(),
                    Headers = headers != null ? new ReadOnlyDictionary<string, string>(headers) : null,
                    JwtBearerTokenFactory = hubConnectionBuilder.GetJwtBearerTokenFactory()
                };

                return new HttpConnection(url,
                    hubConnectionBuilder.GetTransport(),
                    hubConnectionBuilder.GetLoggerFactory(),
                    httpOptions);
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

        public static IHubConnectionBuilder WithHeader(this IHubConnectionBuilder hubConnectionBuilder, string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Header name cannot be null or empty string.", nameof(name));
            }

            var headers = hubConnectionBuilder.GetHeaders();
            if (headers == null)
            {
                headers = new Dictionary<string, string>();
                hubConnectionBuilder.AddSetting(HeadersKey, headers);
            }

            headers.Add(name, value);

            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithJwtBearer(this IHubConnectionBuilder hubConnectionBuilder, Func<string> jwtBearerTokenFactory)
        {
            if (jwtBearerTokenFactory == null)
            {
                throw new ArgumentNullException(nameof(jwtBearerTokenFactory));
            }

            hubConnectionBuilder.AddSetting(JwtBearerTokenFactoryKey, jwtBearerTokenFactory);

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

        public static IDictionary<string, string> GetHeaders(this IHubConnectionBuilder hubConnectionBuilder)
        {
            if (hubConnectionBuilder.TryGetSetting<IDictionary<string, string>>(HeadersKey, out var headers))
            {
                return headers;
            }

            return null;
        }

        public static Func<string> GetJwtBearerTokenFactory(this IHubConnectionBuilder hubConnectionBuilder)
        {
            if (hubConnectionBuilder.TryGetSetting<Func<string>>(JwtBearerTokenFactoryKey, out var factory))
            {
                return factory;
            }

            return null;
        }
    }
}
