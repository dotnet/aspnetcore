// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;
using Microsoft.AspNet.StaticFiles;

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
            return services.AddWebEncoders();
        }
    }
}