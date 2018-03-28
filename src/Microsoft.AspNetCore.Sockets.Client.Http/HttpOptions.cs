// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Sockets.Client.Http
{
    public class HttpOptions
    {
        /// <summary>
        /// Gets or sets a delegate for wrapping or replacing the <see cref="HttpMessageHandler"/>
        /// that will make HTTP requests the server.
        /// </summary>
        public Func<HttpMessageHandler, HttpMessageHandler> HttpMessageHandler { get; set; }

        public IReadOnlyCollection<KeyValuePair<string, string>> Headers { get; set; }
        public Func<string> AccessTokenFactory { get; set; }
        public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public ICredentials Credentials { get; set; }
        public X509CertificateCollection ClientCertificates { get; set; } = new X509CertificateCollection();
        public CookieContainer Cookies { get; set; } = new CookieContainer();
        public IWebProxy Proxy { get; set; }
        public bool? UseDefaultCredentials { get; set; }

        /// <summary>
        /// Gets or sets a delegate that will be invoked with the <see cref="ClientWebSocketOptions"/> object used
        /// to configure the WebSocket when using the WebSockets transport.
        /// </summary>
        /// <remarks>
        /// This delegate is invoked after headers from <see cref="Headers"/> and the access token from <see cref="AccessTokenFactory"/>
        /// has been applied.
        /// </remarks>
        public Action<ClientWebSocketOptions> WebSocketOptions { get; set; }
    }
}
