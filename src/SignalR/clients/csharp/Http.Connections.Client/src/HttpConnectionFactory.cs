// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Connections.Client
{
    /// <summary>
    /// A factory for creating <see cref="HttpConnection"/> instances.
    /// </summary>
    public class HttpConnectionFactory : IConnectionFactory
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
        /// Creates a new connection to an <see cref="UriEndPoint"/>.
        /// </summary>
        /// <param name="endPoint">The <see cref="UriEndPoint"/> to connect to.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>
        /// A <see cref="ValueTask{TResult}" /> that represents the asynchronous connect, yielding the <see cref="ConnectionContext" /> for the new connection when completed.
        /// </returns>
        public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            if (!(endPoint is UriEndPoint uriEndPoint))
            {
                throw new NotSupportedException($"The provided {nameof(EndPoint)} must be of type {nameof(UriEndPoint)}.");
            }

            if (_httpConnectionOptions.Url != null && _httpConnectionOptions.Url != uriEndPoint.Uri)
            {
                throw new InvalidOperationException($"If {nameof(HttpConnectionOptions)}.{nameof(HttpConnectionOptions.Url)} was set, it must match the {nameof(UriEndPoint)}.{nameof(UriEndPoint.Uri)} passed to {nameof(ConnectAsync)}.");
            }

            // Shallow copy before setting the Url property so we don't mutate the user-defined options object.
            var shallowCopiedOptions = ShallowCopyHttpConnectionOptions(_httpConnectionOptions);
            shallowCopiedOptions.Url = uriEndPoint.Uri;

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
            var newOptions = new HttpConnectionOptions
            {
                HttpMessageHandlerFactory = options.HttpMessageHandlerFactory,
                Headers = options.Headers,
                Url = options.Url,
                Transports = options.Transports,
                SkipNegotiation = options.SkipNegotiation,
                AccessTokenProvider = options.AccessTokenProvider,
                CloseTimeout = options.CloseTimeout,
                DefaultTransferFormat = options.DefaultTransferFormat,
            };

            if (!OperatingSystem.IsBrowser())
            {
                newOptions.Cookies = options.Cookies;
                newOptions.ClientCertificates = options.ClientCertificates;
                newOptions.Credentials = options.Credentials;
                newOptions.Proxy = options.Proxy;
                newOptions.UseDefaultCredentials = options.UseDefaultCredentials;
                newOptions.WebSocketConfiguration = options.WebSocketConfiguration;
            }

            return newOptions;
        }
    }
}
