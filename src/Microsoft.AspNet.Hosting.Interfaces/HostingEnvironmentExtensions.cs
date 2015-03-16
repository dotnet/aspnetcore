// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingEnvironmentExtensions
    {
        /// <summary>
        /// Compares the current hosting environment name against the specified value.
        /// </summary>
        /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/> service.</param>
        /// <param name="environmentName">Environment name to validate against.</param>
        /// <returns>True if the specified name is same as the current environment.</returns>
        public static bool IsEnvironment(
            [NotNull]this IHostingEnvironment hostingEnvironment,
            [NotNull]string environmentName)
        {
            return string.Equals(
                hostingEnvironment.EnvironmentName,
                environmentName,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}