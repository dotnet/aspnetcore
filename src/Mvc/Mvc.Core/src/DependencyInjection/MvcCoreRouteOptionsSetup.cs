// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Sets up MVC default options for <see cref="RouteOptions"/>.
/// </summary>
internal sealed class MvcCoreRouteOptionsSetup : IConfigureOptions<RouteOptions>
{
    /// <summary>
    /// Configures the <see cref="RouteOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RouteOptions"/>.</param>
    public void Configure(RouteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ConstraintMap.Add("exists", typeof(KnownRouteValueConstraint));
    }
}
