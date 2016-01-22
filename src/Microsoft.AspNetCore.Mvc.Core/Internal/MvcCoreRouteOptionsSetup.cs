// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// Sets up MVC default options for <see cref="RouteOptions"/>.
    /// </summary>
    public class MvcCoreRouteOptionsSetup : ConfigureOptions<RouteOptions>
    {
        public MvcCoreRouteOptionsSetup()
            : base(ConfigureRouting)
        {
        }

        /// <summary>
        /// Configures the <see cref="RouteOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="RouteOptions"/>.</param>
        public static void ConfigureRouting(RouteOptions options)
        {
            options.ConstraintMap.Add("exists", typeof(KnownRouteValueConstraint));
        }
    }
}
