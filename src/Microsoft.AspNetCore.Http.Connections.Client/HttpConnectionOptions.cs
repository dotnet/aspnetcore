// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Connections.Client
{
    public class HttpConnectionOptions
    {
        private IDictionary<string, string> _headers;
        private X509CertificateCollection _clientCertificates;
        private CookieContainer _cookies;

        public HttpConnectionOptions()
        {
            _headers = new Dictionary<string, string>();
            _clientCertificates = new X509CertificateCollection();
            _cookies = new CookieContainer();

            Transports = HttpTransports.All;
        }

        /// <summary>
        /// Gets or sets a delegate for wrapping or replacing the <see cref="HttpMessageHandlerFactory"/>
        /// that will make HTTP requests the server.
        /// </summary>
        public Func<HttpMessageHandler, HttpMessageHandler> HttpMessageHandlerFactory { get; set; }

        public IDictionary<string, string> Headers
        {
            get => _headers;
            set => _headers = value ?? throw new ArgumentNullException(nameof(value));
        }

        public X509CertificateCollection ClientCertificates
        {
            get => _clientCertificates;
            set => _clientCertificates = value ?? throw new ArgumentNullException(nameof(value));
        }

        public CookieContainer Cookies
        {
            get => _cookies;
            set => _cookies = value ?? throw new ArgumentNullException(nameof(value));
        }

        public Uri Url { get; set; }
        public HttpTransportType Transports { get; set; }
        public Func<Task<string>> AccessTokenProvider { get; set; }
        public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public ICredentials Credentials { get; set; }
        public IWebProxy Proxy { get; set; }
        public bool? UseDefaultCredentials { get; set; }

        /// <summary>
        /// Gets or sets a delegate that will be invoked with the <see cref="ClientWebSocketOptions"/> object used
        /// to configure the WebSocket when using the WebSockets transport.
        /// </summary>
        /// <remarks>
        /// This delegate is invoked after headers from <see cref="Headers"/> and the access token from <see cref="AccessTokenProvider"/>
        /// has been applied.
        /// </remarks>
        public Action<ClientWebSocketOptions> WebSocketConfiguration { get; set; }
    }
}
