// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(Microsoft.AspNetCore.Server.IISIntegration.IISHostingStartup))]

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    /// <summary>
    /// The <see cref="IHostingStartup"/> to add IISIntegration to apps.
    /// </summary>
    public class IISHostingStartup : IHostingStartup
    {
        /// <summary>
        /// Configures the middleware pipeline.
        /// </summary>
        /// <remarks>
        /// Adds IISIntegration into the middleware pipeline.
        /// </remarks>
        /// <param name="builder">The <see cref="IWebHostBuilder"/>.</param>
        public void Configure(IWebHostBuilder builder)
        {
            builder.UseIISIntegration();
        }
    }
}
