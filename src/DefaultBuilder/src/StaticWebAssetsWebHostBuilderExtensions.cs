// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore
{
    /// <summary>
    /// Extensions for configuring static web assets for development.
    /// </summary>
    public static class StaticWebAssetsWebHostBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="IWebHostEnvironment.WebRootFileProvider"/> to use static web assets
        /// defined by referenced projects and packages.
        /// </summary>
        /// <param name="builder">The <see cref="IWebHostBuilder"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder UseStaticWebAssets(this IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                StaticWebAssetsLoader.UseStaticWebAssets(context.HostingEnvironment);
            });

            return builder;
        }
    }
}
