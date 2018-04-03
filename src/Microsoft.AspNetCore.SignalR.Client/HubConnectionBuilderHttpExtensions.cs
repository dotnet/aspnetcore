// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionBuilderHttpExtensions
    {
        public static readonly string TransportTypeKey = "TransportType";
        public static readonly string HttpMessageHandlerKey = "HttpMessageHandler";
        public static readonly string HeadersKey = "Headers";
        public static readonly string AccessTokenFactoryKey = "AccessTokenFactory";
        public static readonly string WebSocketOptionsKey = "WebSocketOptions";
        public static readonly string CookiesKey = "Cookies";
        public static readonly string ProxyKey = "Proxy";
        public static readonly string ClientCertificatesKey = "ClientCertificates";
        public static readonly string CredentialsKey = "Credentials";
        public static readonly string UseDefaultCredentialsKey = "UseDefaultCredentials";

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
                    AccessTokenFactory = hubConnectionBuilder.GetAccessTokenFactory(),
                    WebSocketOptions = hubConnectionBuilder.GetWebSocketOptions(),
                    Cookies = hubConnectionBuilder.GetCookies(),
                    Proxy = hubConnectionBuilder.GetProxy(),
                    UseDefaultCredentials = hubConnectionBuilder.GetUseDefaultCredentials(),
                    ClientCertificates = hubConnectionBuilder.GetClientCertificates(),
                    Credentials = hubConnectionBuilder.GetCredentials(),
                };

                return new HttpConnection(url,
                    hubConnectionBuilder.GetTransport(),
                    hubConnectionBuilder.GetLoggerFactory(),
                    httpOptions);
            });
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithTransport(this IHubConnectionBuilder hubConnectionBuilder, HttpTransportType transportType)
        {
            hubConnectionBuilder.AddSetting(TransportTypeKey, transportType);
            return hubConnectionBuilder;
        }

        /// <summary>
        /// Sets a delegate for wrapping or replacing the <see cref="HttpMessageHandler"/> that will make HTTP requests the server.
        /// </summary>
        /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder"/>.</param>
        /// <param name="configurehttpMessageHandler">A delegate for wrapping or replacing the <see cref="HttpMessageHandler"/> that will make HTTP requests the server.</param>
        /// <returns>The <see cref="IHubConnectionBuilder"/>.</returns>
        public static IHubConnectionBuilder WithMessageHandler(this IHubConnectionBuilder hubConnectionBuilder, Func<HttpMessageHandler, HttpMessageHandler> configurehttpMessageHandler)
        {
            hubConnectionBuilder.AddSetting(HttpMessageHandlerKey, configurehttpMessageHandler);
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

        public static IHubConnectionBuilder WithUseDefaultCredentials(this IHubConnectionBuilder hubConnectionBuilder, bool useDefaultCredentials)
        {
            hubConnectionBuilder.AddSetting<bool?>(UseDefaultCredentialsKey, useDefaultCredentials);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithCredentials(this IHubConnectionBuilder hubConnectionBuilder, ICredentials credentials)
        {
            hubConnectionBuilder.AddSetting(CredentialsKey, credentials);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithProxy(this IHubConnectionBuilder hubConnectionBuilder, IWebProxy proxy)
        {
            hubConnectionBuilder.AddSetting(ProxyKey, proxy);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithCookie(this IHubConnectionBuilder hubConnectionBuilder, Cookie cookie)
        {
            if (cookie == null)
            {
                throw new ArgumentNullException(nameof(cookie));
            }

            var cookies = hubConnectionBuilder.GetCookies();
            if (cookies == null)
            {
                cookies = new CookieContainer();
                hubConnectionBuilder.AddSetting(CookiesKey, cookies);
            }

            cookies.Add(cookie);

            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithClientCertificate(this IHubConnectionBuilder hubConnectionBuilder, X509Certificate clientCertificate)
        {
            if (clientCertificate == null)
            {
                throw new ArgumentNullException(nameof(clientCertificate));
            }

            var clientCertificates = hubConnectionBuilder.GetClientCertificates();
            if (clientCertificates == null)
            {
                clientCertificates = new X509CertificateCollection();
                hubConnectionBuilder.AddSetting(ClientCertificatesKey, clientCertificates);
            }

            clientCertificates.Add(clientCertificate);

            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithAccessToken(this IHubConnectionBuilder hubConnectionBuilder, Func<string> accessTokenFactory)
        {
            if (accessTokenFactory == null)
            {
                throw new ArgumentNullException(nameof(accessTokenFactory));
            }

            hubConnectionBuilder.AddSetting(AccessTokenFactoryKey, accessTokenFactory);

            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithWebSocketOptions(this IHubConnectionBuilder hubConnectionBuilder, Action<ClientWebSocketOptions> configureWebSocketOptions)
        {
            if (configureWebSocketOptions == null)
            {
                throw new ArgumentNullException(nameof(configureWebSocketOptions));
            }

            hubConnectionBuilder.AddSetting(WebSocketOptionsKey, configureWebSocketOptions);

            return hubConnectionBuilder;
        }

        public static HttpTransportType GetTransport(this IHubConnectionBuilder hubConnectionBuilder)
        {
            if (hubConnectionBuilder.TryGetSetting<HttpTransportType>(TransportTypeKey, out var transportType))
            {
                return transportType;
            }

            return HttpTransportType.All;
        }

        /// <summary>
        /// Gets a delegate for wrapping or replacing the <see cref="HttpMessageHandler"/> that will make HTTP requests the server.
        /// </summary>
        /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder"/>.</param>
        /// <returns>A delegate for wrapping or replacing the <see cref="HttpMessageHandler"/> that will make HTTP requests the server.</returns>
        public static Func<HttpMessageHandler, HttpMessageHandler> GetMessageHandler(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<Func<HttpMessageHandler, HttpMessageHandler>>(HttpMessageHandlerKey, out var messageHandler);
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

        public static IWebProxy GetProxy(this IHubConnectionBuilder hubConnectionBuilder)
        {
            if (hubConnectionBuilder.TryGetSetting<IWebProxy>(ProxyKey, out var proxy))
            {
                return proxy;
            }

            return null;
        }

        public static bool? GetUseDefaultCredentials(this IHubConnectionBuilder hubConnectionBuilder)
        {
            if (hubConnectionBuilder.TryGetSetting<bool?>(UseDefaultCredentialsKey, out var useDefaultCredentials))
            {
                return useDefaultCredentials;
            }

            return null;
        }

        public static CookieContainer GetCookies(this IHubConnectionBuilder hubConnectionBuilder)
        {
            if (hubConnectionBuilder.TryGetSetting<CookieContainer>(CookiesKey, out var cookies))
            {
                return cookies;
            }

            return null;
        }

        public static ICredentials GetCredentials(this IHubConnectionBuilder hubConnectionBuilder)
        {
            if (hubConnectionBuilder.TryGetSetting<ICredentials>(CredentialsKey, out var credentials))
            {
                return credentials;
            }

            return null;
        }

        public static X509CertificateCollection GetClientCertificates(this IHubConnectionBuilder hubConnectionBuilder)
        {
            if (hubConnectionBuilder.TryGetSetting<X509CertificateCollection>(ClientCertificatesKey, out var clientCertificates))
            {
                return clientCertificates;
            }

            return null;
        }

        public static Func<string> GetAccessTokenFactory(this IHubConnectionBuilder hubConnectionBuilder)
        {
            if (hubConnectionBuilder.TryGetSetting<Func<string>>(AccessTokenFactoryKey, out var factory))
            {
                return factory;
            }

            return null;
        }

        public static Action<ClientWebSocketOptions> GetWebSocketOptions(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<Action<ClientWebSocketOptions>>(WebSocketOptionsKey, out var webSocketOptions);
            return webSocketOptions;
        }
    }
}
