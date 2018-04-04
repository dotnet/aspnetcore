// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http.Connections;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public class HttpConnectionOptions
    {
        internal X509CertificateCollection _clientCertificates;
        internal IDictionary<string, string> _headers;
        internal CookieContainer _cookies;

        public Uri Url { get; set; }
        public HttpTransportType? Transport { get; set; }
        public Func<HttpMessageHandler, HttpMessageHandler> MessageHandlerFactory { get; set; }
        public bool? UseDefaultCredentials { get; set; }
        public ICredentials Credentials { get; set; }
        public IWebProxy Proxy { get; set; }
        public Func<string> AccessTokenFactory { get; set; }
        public Action<ClientWebSocketOptions> WebSocketOptions { get; set; }

        public X509CertificateCollection ClientCertificates
        {
            get
            {
                if (_clientCertificates == null)
                {
                    _clientCertificates = new X509CertificateCollection();
                }

                return _clientCertificates;
            }
        }

        public CookieContainer Cookies
        {
            get
            {
                if (_cookies == null)
                {
                    _cookies = new CookieContainer();
                }

                return _cookies;
            }
        }

        public IDictionary<string, string> Headers
        {
            get
            {
                if (_headers == null)
                {
                    _headers = new Dictionary<string, string>();
                }

                return _headers;
            }
        }
    }
}