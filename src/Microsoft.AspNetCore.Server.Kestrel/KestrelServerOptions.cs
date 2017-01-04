// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    /// <summary>
    /// Provides programmatic configuration of Kestrel-specific features.
    /// </summary>
    public class KestrelServerOptions
    {
        /// <summary>
        /// Configures the endpoints that Kestrel should listen to.
        /// </summary>
        /// <remarks>
        /// If this list is empty, the server.urls setting (e.g. UseUrls) is used.
        /// </remarks>
        internal List<ListenOptions> ListenOptions { get; } = new List<ListenOptions>();

        /// <summary>
        /// Gets or sets whether the <c>Server</c> header should be included in each response.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool AddServerHeader { get; set; } = true;

        /// <summary>
        /// Enables the Listen options callback to resolve and use services registered by the application during startup.
        /// Typically initialized by <see cref="Hosting.WebHostBuilderKestrelExtensions.UseKestrel(Hosting.IWebHostBuilder, Action{KestrelServerOptions})"/>.
        /// </summary>
        public IServiceProvider ApplicationServices { get; set; }

        /// <summary>
        /// <para>
        /// This property is obsolete and will be removed in a future version.
        /// Use <c>Limits.MaxRequestBufferSize</c> instead.
        /// </para>
        /// <para>
        /// Gets or sets the maximum size of the request buffer.
        /// </para>
        /// </summary>
        /// <remarks>
        /// When set to null, the size of the request buffer is unlimited.
        /// Defaults to 1,048,576 bytes (1 MB).
        /// </remarks>
        [Obsolete("This property is obsolete and will be removed in a future version. Use Limits.MaxRequestBufferSize instead.")]
        public long? MaxRequestBufferSize
        {
            get
            {
                return Limits.MaxRequestBufferSize;
            }
            set
            {
                Limits.MaxRequestBufferSize = value;
            }
        }

        /// <summary>
        /// Provides access to request limit options.
        /// </summary>
        public KestrelServerLimits Limits { get; } = new KestrelServerLimits();

        /// <summary>
        /// The amount of time after the server begins shutting down before connections will be forcefully closed.
        /// Kestrel will wait for the duration of the timeout for any ongoing request processing to complete before
        /// terminating the connection. No new connections or requests will be accepted during this time.
        /// </summary>
        /// <remarks>
        /// Defaults to 5 seconds.
        /// </remarks>
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The number of libuv I/O threads used to process requests.
        /// </summary>
        /// <remarks>
        /// Defaults to half of <see cref="Environment.ProcessorCount" /> rounded down and clamped between 1 and 16.
        /// </remarks>
        public int ThreadCount { get; set; } = ProcessorThreadCount;

        private static int ProcessorThreadCount
        {
            get
            {
                // Actual core count would be a better number
                // rather than logical cores which includes hyper-threaded cores.
                // Divide by 2 for hyper-threading, and good defaults (still need threads to do webserving).
                var threadCount = Environment.ProcessorCount >> 1;

                if (threadCount < 1)
                {
                    // Ensure shifted value is at least one
                    return 1;
                }

                if (threadCount > 16)
                {
                    // Receive Side Scaling RSS Processor count currently maxes out at 16
                    // would be better to check the NIC's current hardware queues; but xplat...
                    return 16;
                }

                return threadCount;
            }
        }

        /// <summary>
        /// Bind to given IP address and port.
        /// </summary>
        public void Listen(IPAddress address, int port)
        {
            Listen(address, port, _ => { });
        }

        /// <summary>
        /// Bind to given IP address and port.
        /// The callback configures endpoint-specific settings.
        /// </summary>
        public void Listen(IPAddress address, int port, Action<ListenOptions> configure)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            Listen(new IPEndPoint(address, port), configure);
        }

        /// <summary>
        /// Bind to given IP endpoint.
        /// </summary>
        public void Listen(IPEndPoint endPoint)
        {
            Listen(endPoint, _ => { });
        }

        /// <summary>
        /// Bind to given IP address and port.
        /// The callback configures endpoint-specific settings.
        /// </summary>
        public void Listen(IPEndPoint endPoint, Action<ListenOptions> configure)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var listenOptions = new ListenOptions(endPoint) { KestrelServerOptions = this };
            configure(listenOptions);
            ListenOptions.Add(listenOptions);
        }

        /// <summary>
        /// Bind to given Unix domain socket path.
        /// </summary>
        public void ListenUnixSocket(string socketPath)
        {
            ListenUnixSocket(socketPath, _ => { });
        }

        /// <summary>
        /// Bind to given Unix domain socket path.
        /// Specify callback to configure endpoint-specific settings.
        /// </summary>
        public void ListenUnixSocket(string socketPath, Action<ListenOptions> configure)
        {
            if (socketPath == null)
            {
                throw new ArgumentNullException(nameof(socketPath));
            }
            if (socketPath.Length == 0 || socketPath[0] != '/')
            {
                throw new ArgumentException("Unix socket path must be absolute.", nameof(socketPath));
            }
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var listenOptions = new ListenOptions(socketPath) { KestrelServerOptions = this };
            configure(listenOptions);
            ListenOptions.Add(listenOptions);
        }

        /// <summary>
        /// Open a socket file descriptor.
        /// </summary>
        public void ListenHandle(ulong handle)
        {
            ListenHandle(handle, _ => { });
        }

        /// <summary>
        /// Open a socket file descriptor.
        /// The callback configures endpoint-specific settings.
        /// </summary>
        public void ListenHandle(ulong handle, Action<ListenOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var listenOptions = new ListenOptions(handle) { KestrelServerOptions = this };
            configure(listenOptions);
            ListenOptions.Add(listenOptions);
        }
    }
}
