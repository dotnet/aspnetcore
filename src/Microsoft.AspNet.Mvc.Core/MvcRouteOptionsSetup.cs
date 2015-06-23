// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Sets up MVC default options for <see cref="RouteOptions"/>.
    /// </summary>
    public class MvcRouteOptionsSetup : ConfigureOptions<RouteOptions>
    {
        public MvcRouteOptionsSetup()
            : base(ConfigureRouting)
        {
            Order = DefaultOrder.DefaultFrameworkSortOrder;
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
