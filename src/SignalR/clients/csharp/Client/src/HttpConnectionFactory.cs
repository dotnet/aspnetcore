// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Client
{
    /// <summary>
    /// A factory for creating <see cref="HttpConnection"/> instances.
    /// </summary>
    internal class HttpConnectionFactory : IConnectionFactory
    {
        private readonly HttpConnectionOptions _httpConnectionOptions;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpConnectionFactory"/> class.
        /// </summary>
        /// <param name="options">The connection options.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public HttpConnectionFactory(IOptions<HttpConnectionOptions> options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _httpConnectionOptions = options.Value;
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Creates a new connection to an <see cref="HttpEndPoint"/>.
        /// </summary>
        /// <param name="httpEndPoint">The <see cref="HttpEndPoint"/> to connect to.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>
        /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous connect.
        /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ConnectionContext"/> for the new connection.
        /// </returns>
        public async ValueTask<ConnectionContext> ConnectAsync(EndPoint httpEndPoint, CancellationToken cancellationToken = default)
        {
            if (httpEndPoint == null)
            {
                throw new ArgumentNullException(nameof(httpEndPoint));
            }

            var castedHttpEndPoint = httpEndPoint as HttpEndPoint;
            if (castedHttpEndPoint == null)
            {
                throw new NotSupportedException($"The provided {nameof(EndPoint)} must be of type {nameof(HttpEndPoint)}.");
            }

            if (_httpConnectionOptions.Url != null && _httpConnectionOptions.Url != castedHttpEndPoint.Url)
            {
                throw new InvalidOperationException($"If {nameof(HttpConnectionOptions)}.{nameof(HttpConnectionOptions.Url)} was set, it must match the {nameof(HttpEndPoint)}.{nameof(HttpEndPoint.Url)} passed to {nameof(ConnectAsync)}.");
            }

            // Shallow copy before setting the Url property so we don't mutate the user-defined options object.
            var shallowCopiedOptions = ShallowCopyHttpConnectionOptions(_httpConnectionOptions);
            shallowCopiedOptions.Url = castedHttpEndPoint.Url;

            var connection = new HttpConnection(shallowCopiedOptions, _loggerFactory);

            try
            {
                await connection.StartAsync(cancellationToken);
                return connection;
            }
            catch
            {
                // Make sure the connection is disposed, in case it allocated any resources before failing.
                await connection.DisposeAsync();
                throw;
            }
        }

        // Internal for testing
        internal static HttpConnectionOptions ShallowCopyHttpConnectionOptions(HttpConnectionOptions options)
        {
            return new HttpConnectionOptions
            {
                HttpMessageHandlerFactory = options.HttpMessageHandlerFactory,
                Headers = options.Headers,
                ClientCertificates = options.ClientCertificates,
                Cookies = options.Cookies,
                Url = options.Url,
                Transports = options.Transports,
                SkipNegotiation = options.SkipNegotiation,
                AccessTokenProvider = options.AccessTokenProvider,
                CloseTimeout = options.CloseTimeout,
                Credentials = options.Credentials,
                Proxy = options.Proxy,
                UseDefaultCredentials = options.UseDefaultCredentials,
                DefaultTransferFormat = options.DefaultTransferFormat,
                WebSocketConfiguration = options.WebSocketConfiguration,
            };
        }
    }
}
