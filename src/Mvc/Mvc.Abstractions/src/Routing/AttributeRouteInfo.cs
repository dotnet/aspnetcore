// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// Represents the routing information for an action that is attribute routed.
/// </summary>
public class AttributeRouteInfo
{
    /// <summary>
    /// The route template. May be <see langword="null" /> if the action has no attribute routes.
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Gets the order of the route associated with a given action. This property determines
    /// the order in which routes get executed. Routes with a lower order value are tried first. In case a route
    /// doesn't specify a value, it gets a default order of 0.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets the name of the route associated with a given action. This property can be used
    /// to generate a link by referring to the route by name instead of attempting to match a
    /// route by provided route data.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if the route entry associated with this model participates in link generation.
    /// </summary>
    public bool SuppressLinkGeneration { get; set; }

    /// <summary>
    /// Gets or sets a value that determines if the route entry associated with this model participates in path matching (inbound routing).
    /// </summary>
    public bool SuppressPathMatching { get; set; }
}
