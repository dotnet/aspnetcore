// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public interface IEndPointInformation
    {
        /// <summary>
        /// The type of interface being described: either an <see cref="IPEndPoint"/>, Unix domain socket path, or a file descriptor.
        /// </summary>
        ListenType Type { get; }

        // IPEndPoint is mutable so port 0 can be updated to the bound port.
        /// <summary>
        /// The <see cref="IPEndPoint"/> to bind to.
        /// Only set if <see cref="Type"/> is <see cref="ListenType.IPEndPoint"/>.
        /// </summary>
        IPEndPoint IPEndPoint { get; set; }

        /// <summary>
        /// The absolute path to a Unix domain socket to bind to.
        /// Only set if <see cref="Type"/> is <see cref="ListenType.SocketPath"/>.
        /// </summary>
        string SocketPath { get; }

        /// <summary>
        /// A file descriptor for the socket to open.
        /// Only set if <see cref="Type"/> is <see cref="ListenType.FileHandle"/>.
        /// </summary>
        ulong FileHandle { get; }

        //  HandleType is mutable so it can be re-specified later.
        /// <summary>
        /// The type of file descriptor being used.
        /// Only set if <see cref="Type"/> is <see cref="ListenType.FileHandle"/>.
        /// </summary>
        FileHandleType HandleType { get; set; }

        /// <summary>
        /// Set to false to enable Nagle's algorithm for all connections.
        /// </summary>
        bool NoDelay { get; }
    }
}
