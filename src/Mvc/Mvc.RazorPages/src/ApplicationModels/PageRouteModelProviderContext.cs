// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// A context object for <see cref="IPageRouteModelProvider"/>.
/// </summary>
public class PageRouteModelProviderContext
{
    /// <summary>
    /// Gets the <see cref="PageRouteModel"/> instances.
    /// </summary>
    public IList<PageRouteModel> RouteModels { get; } = new List<PageRouteModel>();
}
