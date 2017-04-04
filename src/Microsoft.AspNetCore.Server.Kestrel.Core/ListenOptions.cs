// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    /// <summary>
    /// Describes either an <see cref="IPEndPoint"/>, Unix domain socket path, or a file descriptor for an already open
    /// socket that Kestrel should bind to or open.
    /// </summary>
    public class ListenOptions : IEndPointInformation
    {
        internal ListenOptions(IPEndPoint endPoint)
        {
            Type = ListenType.IPEndPoint;
            IPEndPoint = endPoint;
        }

        internal ListenOptions(string socketPath)
        {
            Type = ListenType.SocketPath;
            SocketPath = socketPath;
        }

        internal ListenOptions(ulong fileHandle)
        {
            Type = ListenType.FileHandle;
            FileHandle = fileHandle;
        }

        /// <summary>
        /// The type of interface being described: either an <see cref="IPEndPoint"/>, Unix domain socket path, or a file descriptor.
        /// </summary>
        public ListenType Type { get; }

        // IPEndPoint is mutable so port 0 can be updated to the bound port.
        /// <summary>
        /// The <see cref="IPEndPoint"/> to bind to.
        /// Only set if the <see cref="ListenOptions"/> <see cref="Type"/> is <see cref="ListenType.IPEndPoint"/>.
        /// </summary>
        public IPEndPoint IPEndPoint { get; set; }

        /// <summary>
        /// The absolute path to a Unix domain socket to bind to.
        /// Only set if the <see cref="ListenOptions"/> <see cref="Type"/> is <see cref="ListenType.SocketPath"/>.
        /// </summary>
        public string SocketPath { get; }

        /// <summary>
        /// A file descriptor for the socket to open.
        /// Only set if the <see cref="ListenOptions"/> <see cref="Type"/> is <see cref="ListenType.FileHandle"/>.
        /// </summary>
        public ulong FileHandle { get; }

        /// <summary>
        /// Enables an <see cref="IConnectionAdapter"/> to resolve and use services registered by the application during startup.
        /// Only set if accessed from the callback of a <see cref="KestrelServerOptions"/> Listen* method.
        /// </summary>
        public KestrelServerOptions KestrelServerOptions { get; internal set; }

        /// <summary>
        /// Set to false to enable Nagle's algorithm for all connections.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool NoDelay { get; set; } = true;

        /// <summary>
        /// Gets the <see cref="List{IConnectionAdapter}"/> that allows each connection <see cref="System.IO.Stream"/>
        /// to be intercepted and transformed.
        /// Configured by the <c>UseHttps()</c> and <see cref="Hosting.ListenOptionsConnectionLoggingExtensions.UseConnectionLogging(ListenOptions)"/>
        /// extension methods.
        /// </summary>
        /// <remarks>
        /// Defaults to empty.
        /// </remarks>
        public List<IConnectionAdapter> ConnectionAdapters { get; } = new List<IConnectionAdapter>();

        // Scheme is hopefully only a temporary measure for back compat with IServerAddressesFeature.
        // TODO: Allow connection adapters to configure the scheme
        public string Scheme { get; set; } = "http";

        public override string ToString()
        {
            // Use http scheme for all addresses. If https should be used for this endPoint,
            // it can still be configured for this endPoint specifically.
            switch (Type)
            {
                case ListenType.IPEndPoint:
                    return $"{Scheme}://{IPEndPoint}";
                case ListenType.SocketPath:
                    return $"{Scheme}://unix:{SocketPath}";
                case ListenType.FileHandle:
                    // This was never supported via --server.urls, so no need to include Scheme.
                    return "http://<file handle>";
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
