// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingEnvironmentExtensions
    {
        /// <summary>
        /// Checks if the current hosting environment name is development.
        /// </summary>
        /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/> service.</param>
        /// <returns>True if the environment name is Development, otherwise false.</returns>
        public static bool IsDevelopment(this IHostingEnvironment hostingEnvironment)
        {
            if (hostingEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostingEnvironment));
            }

            return hostingEnvironment.IsEnvironment(EnvironmentName.Development);
        }

        /// <summary>
        /// Checks if the current hosting environment name is Production.
        /// </summary>
        /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/> service.</param>
        /// <returns>True if the environment name is Production, otherwise false.</returns>
        public static bool IsProduction(this IHostingEnvironment hostingEnvironment)
        {
            if (hostingEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostingEnvironment));
            }

            return hostingEnvironment.IsEnvironment(EnvironmentName.Production);
        }

        /// <summary>
        /// Compares the current hosting environment name against the specified value.
        /// </summary>
        /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/> service.</param>
        /// <param name="environmentName">Environment name to validate against.</param>
        /// <returns>True if the specified name is same as the current environment.</returns>
        public static bool IsEnvironment(
            this IHostingEnvironment hostingEnvironment,
            string environmentName)
        {
            if (hostingEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostingEnvironment));
            }

            return string.Equals(
                hostingEnvironment.EnvironmentName,
                environmentName,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gives the physical path corresponding to the given virtual path.
        /// </summary>
        /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/> service.</param>
        /// <param name="virtualPath">Path relative to the root.</param>
        /// <returns>Physical path corresponding to virtual path.</returns>
        public static string MapPath(
            this IHostingEnvironment hostingEnvironment,
            string virtualPath)
        {
            if (hostingEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostingEnvironment));
            }

            if (virtualPath == null)
            {
                return hostingEnvironment.WebRootPath;
            }

            // On windows replace / with \.
            virtualPath = virtualPath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(hostingEnvironment.WebRootPath, virtualPath);
        }
    }
}