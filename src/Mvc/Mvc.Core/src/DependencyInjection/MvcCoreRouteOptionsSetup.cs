// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Sets up MVC default options for <see cref="RouteOptions"/>.
    /// </summary>
    internal class MvcCoreRouteOptionsSetup : IConfigureOptions<RouteOptions>
    {
        /// <summary>
        /// Configures the <see cref="RouteOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="RouteOptions"/>.</param>
        public void Configure(RouteOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.ConstraintMap.Add("exists", typeof(KnownRouteValueConstraint));
        }
    }
}
