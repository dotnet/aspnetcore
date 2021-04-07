// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Sockets;

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// Provides access to the connection's underlying <see cref="Socket"/> if any.
    /// </summary>
    public interface IConnectionSocketFeature
    {
        /// <summary>
        /// Gets the underlying <see cref="Socket"/>.
        /// </summary>
        Socket? Socket { get; }
    }
}
