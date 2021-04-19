// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Connections
{
    /// <summary>
    /// Defines an interface that provides the mechanisms to configure a connection pipeline.
    /// </summary>
    public interface IMultiplexedConnectionBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> that provides access to the application's service container.
        /// </summary>
        IServiceProvider ApplicationServices { get; }

        /// <summary>
        /// Adds a middleware delegate to the application's connection pipeline.
        /// </summary>
        /// <param name="middleware">The middleware delegate.</param>
        /// <returns>The <see cref="IMultiplexedConnectionBuilder"/>.</returns>
        IMultiplexedConnectionBuilder Use(Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate> middleware);

        /// <summary>
        /// Builds the delegate used by this application to process connections.
        /// </summary>
        /// <returns>The connection handling delegate.</returns>
        MultiplexedConnectionDelegate Build();
    }
}
