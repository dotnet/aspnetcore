// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// A feature that represents a connection endpoints.
    /// </summary>
    public interface IConnectionEndPointFeature
    {
        /// <summary>
        /// Gets or sets the local <see cref="EndPoint"/>.
        /// </summary>
        EndPoint? LocalEndPoint { get; set; }

        /// <summary>
        /// Gets or sets the remote <see cref="EndPoint"/>.
        /// </summary>
        EndPoint? RemoteEndPoint { get; set; }
    }
}
