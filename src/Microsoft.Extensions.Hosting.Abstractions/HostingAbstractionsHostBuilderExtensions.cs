// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.Extensions.Hosting
{
    public static class HostingAbstractionsHostBuilderExtensions
    {
        /// <summary>
        /// Start the host and listen on the specified urls.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to start.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHost Start(this IHostBuilder hostBuilder)
        {
            var host = hostBuilder.Build();
            host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            return host;
        }
    }
}
