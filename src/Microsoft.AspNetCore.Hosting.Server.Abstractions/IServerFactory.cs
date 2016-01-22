// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting.Server
{
    /// <summary>
    /// Represents a factory for creating servers.
    /// </summary>
    public interface IServerFactory
    {
        /// <summary>
        /// Creates <see cref="IServer"/> based on the given configuration.
        /// </summary>
        /// <param name="configuration">An instance of <see cref="IConfiguration"/>.</param>
        /// <returns>The created server.</returns>
        IServer CreateServer(IConfiguration configuration);
    }
}
