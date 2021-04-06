// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// Represents the lifetime of the connection.
    /// </summary>
    public interface IConnectionLifetimeFeature
    {
        /// <summary>
        /// Gets or sets the <see cref="CancellationToken"/> that is triggered when the connection is closed.
        /// </summary>
        CancellationToken ConnectionClosed { get; set; }

        /// <summary>
        /// Terminates the current connection.
        /// </summary>
        void Abort();
    }
}
