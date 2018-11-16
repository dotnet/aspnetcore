// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    public static class HostingAbstractionsHostBuilderExtensions
    {
        /// <summary>
        /// Builds and starts the host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to start.</param>
        /// <returns>The started <see cref="IHost"/>.</returns>
        public static IHost Start(this IHostBuilder hostBuilder)
        {
            return hostBuilder.StartAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Builds and starts the host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to start.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The started <see cref="IHost"/>.</returns>
        public static async Task<IHost> StartAsync(this IHostBuilder hostBuilder, CancellationToken cancellationToken = default)
        {
            var host = hostBuilder.Build();
            await host.StartAsync(cancellationToken);
            return host;
        }
    }
}
