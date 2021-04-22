// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// The transport for the connection.
    /// </summary>
    public interface IConnectionTransportFeature
    {
        /// <summary>
        /// Gets or sets the transport for the connection.
        /// </summary>
        IDuplexPipe Transport { get; set; }
    }
}
