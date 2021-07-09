// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    /// <summary>
    /// Kestrel server.
    /// </summary>
    public class KestrelServer : IServer
    {
        private readonly KestrelServerImpl _innerKestrelServer;

        /// <summary>
        /// Initializes a new instance of <see cref="KestrelServer"/>.
        /// </summary>
        /// <param name="options">The Kestrel <see cref="IOptions{TOptions}"/>.</param>
        /// <param name="transportFactory">The <see cref="IConnectionListenerFactory"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public KestrelServer(IOptions<KestrelServerOptions> options, IConnectionListenerFactory transportFactory, ILoggerFactory loggerFactory)
        {
            _innerKestrelServer = new KestrelServerImpl(
                options,
                new[] { transportFactory ?? throw new ArgumentNullException(nameof(transportFactory)) },
                loggerFactory);
        }

        /// <inheritdoc />
        public IFeatureCollection Features => _innerKestrelServer.Features;

        /// <summary>
        /// Gets the <see cref="KestrelServerOptions"/>.
        /// </summary>
        public KestrelServerOptions Options => _innerKestrelServer.Options;

        /// <inheritdoc />
        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
        {
            return _innerKestrelServer.StartAsync(application, cancellationToken);
        }

        // Graceful shutdown if possible
        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _innerKestrelServer.StopAsync(cancellationToken);
        }

        // Ungraceful shutdown
        /// <inheritdoc />
        public void Dispose()
        {
            _innerKestrelServer.Dispose();
        }
    }
}
