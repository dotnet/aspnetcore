// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Connections
{
    /// <summary>
    /// Sets up <see cref="ConnectionOptions"/>.
    /// </summary>
    public class ConnectionOptionsSetup : IConfigureOptions<ConnectionOptions>
    {
        /// <summary>
        /// Default timeout value for disconnecting idle connections.
        /// </summary>
        public static TimeSpan DefaultDisconectTimeout = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Sets default values for options if they have not been set yet.
        /// </summary>
        /// <param name="options">The <see cref="ConnectionOptions"/>.</param>
        public void Configure(ConnectionOptions options)
        {
            if (options.DisconnectTimeout == null)
            {
                options.DisconnectTimeout = DefaultDisconectTimeout;
            }
        }
    }
}
