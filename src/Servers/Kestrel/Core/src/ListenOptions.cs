// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    /// <summary>
    /// Describes either an <see cref="IPEndPoint"/>, Unix domain socket path, or a file descriptor for an already open
    /// socket that Kestrel should bind to or open.
    /// </summary>
    public class ListenOptions : IConnectionBuilder, IMultiplexedConnectionBuilder
    {
        internal readonly List<Func<ConnectionDelegate, ConnectionDelegate>> _middleware = new List<Func<ConnectionDelegate, ConnectionDelegate>>();
        internal readonly List<Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate>> _multiplexedMiddleware = new List<Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate>>();

        internal ListenOptions(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        internal ListenOptions(string socketPath)
        {
            EndPoint = new UnixDomainSocketEndPoint(socketPath);
        }

        internal ListenOptions(ulong fileHandle)
            : this(fileHandle, FileHandleType.Auto)
        {
        }

        internal ListenOptions(ulong fileHandle, FileHandleType handleType)
        {
            EndPoint = new FileHandleEndPoint(fileHandle, handleType);
        }

        internal EndPoint EndPoint { get; set; }

        // IPEndPoint is mutable so port 0 can be updated to the bound port.
        /// <summary>
        /// The <see cref="IPEndPoint"/> to bind to.
        /// Only set if the <see cref="ListenOptions"/> <see cref="Type"/> is <see cref="IPEndPoint"/>.
        /// </summary>
        public IPEndPoint IPEndPoint => EndPoint as IPEndPoint;

        /// <summary>
        /// The absolute path to a Unix domain socket to bind to.
        /// Only set if the <see cref="ListenOptions"/> <see cref="Type"/> is <see cref="UnixDomainSocketEndPoint"/>.
        /// </summary>
        public string SocketPath => (EndPoint as UnixDomainSocketEndPoint)?.ToString();

        /// <summary>
        /// A file descriptor for the socket to open.
        /// Only set if the <see cref="ListenOptions"/> <see cref="Type"/> is <see cref="FileHandleEndPoint"/>.
        /// </summary>
        public ulong FileHandle => (EndPoint as FileHandleEndPoint)?.FileHandle ?? 0;

        /// <summary>
        /// Enables connection middleware to resolve and use services registered by the application during startup.
        /// Only set if accessed from the callback of a <see cref="KestrelServerOptions"/> Listen* method.
        /// </summary>
        public KestrelServerOptions KestrelServerOptions { get; internal set; }

        /// <summary>
        /// The protocols enabled on this endpoint.
        /// </summary>
        /// <remarks>Defaults to HTTP/1.x and HTTP/2.</remarks>
        public HttpProtocols Protocols { get; set; } = HttpProtocols.Http1AndHttp2;

        public IServiceProvider ApplicationServices => KestrelServerOptions?.ApplicationServices;

        internal string Scheme
        {
            get
            {
                if (IsHttp)
                {
                    return IsTls ? "https" : "http";
                }
                return "tcp";
            }
        }

        internal bool IsHttp { get; set; } = true;

        internal bool IsTls { get; set; }

        /// <summary>
        /// Gets the name of this endpoint to display on command-line when the web server starts.
        /// </summary>
        internal virtual string GetDisplayName()
        {
            switch (EndPoint)
            {
                case IPEndPoint _:
                    return $"{Scheme}://{IPEndPoint}";
                case UnixDomainSocketEndPoint _:
                    return $"{Scheme}://unix:{EndPoint}";
                case FileHandleEndPoint _:
                    return $"{Scheme}://<file handle>";
                default:
                    throw new InvalidOperationException();
            }
        }

        public override string ToString() => GetDisplayName();

        /// <summary>
        /// Adds a middleware delegate to the connection pipeline.
        /// Configured by the <c>UseHttps()</c> and <see cref="Hosting.ListenOptionsConnectionLoggingExtensions.UseConnectionLogging(ListenOptions)"/>
        /// extension methods.
        /// </summary>
        /// <param name="middleware">The middleware delegate.</param>
        /// <returns>The <see cref="IConnectionBuilder"/>.</returns>
        public IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware)
        {
            _middleware.Add(middleware);
            return this;
        }

        IMultiplexedConnectionBuilder IMultiplexedConnectionBuilder.Use(Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate> middleware)
        {
            _multiplexedMiddleware.Add(middleware);
            return this;
        }

        public ConnectionDelegate Build()
        {
            ConnectionDelegate app = context =>
            {
                return Task.CompletedTask;
            };

            for (int i = _middleware.Count - 1; i >= 0; i--)
            {
                var component = _middleware[i];
                app = component(app);
            }

            return app;
        }

        MultiplexedConnectionDelegate IMultiplexedConnectionBuilder.Build()
        {
            MultiplexedConnectionDelegate app = context =>
            {
                return Task.CompletedTask;
            };

            for (int i = _multiplexedMiddleware.Count - 1; i >= 0; i--)
            {
                var component = _multiplexedMiddleware[i];
                app = component(app);
            }

            return app;
        }

        internal virtual async Task BindAsync(AddressBindContext context)
        {
            await AddressBinder.BindEndpointAsync(this, context).ConfigureAwait(false);
            context.Addresses.Add(GetDisplayName());
        }
    }
}
