// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.StaticFiles;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding directory browser services.
    /// </summary>
    public static class DirectoryBrowserServiceExtensions
    {
        /// <summary>
        /// Adds directory browser middleware services.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDirectoryBrowser([NotNull] this IServiceCollection services)
        {
            return services.AddDirectoryBrowser(configuration: null);
        }

        /// <summary>
        /// Adds directory browser middleware services.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddDirectoryBrowser([NotNull] this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddEncoders(configuration);
        }
    }
}