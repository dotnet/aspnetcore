// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Defines a contract to parse URIs using information from routing.
/// </summary>
public abstract class LinkParser
{
    /// <summary>
    /// Attempts to parse the provided <paramref name="path"/> using the route pattern
    /// specified by the <see cref="Endpoint"/> matching <paramref name="address"/>.
    /// </summary>
    /// <typeparam name="TAddress">The address type.</typeparam>
    /// <param name="address">The address value. Used to resolve endpoints.</param>
    /// <param name="path">The URI path to parse.</param>
    /// <returns>
    /// A <see cref="RouteValueDictionary"/> with the parsed values if parsing is successful;
    /// otherwise <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <see cref="ParsePathByAddress{TAddress}(TAddress, PathString)"/> will attempt to first resolve
    /// <see cref="Endpoint"/> instances that match <paramref name="address"/> and then use the route
    /// pattern associated with each endpoint to parse the URL path.
    /// </para>
    /// <para>
    /// The parsing operation will fail and return <c>null</c> if either no endpoints are found or none
    /// of the route patterns match the provided URI path.
    /// </para>
    /// </remarks>
    public abstract RouteValueDictionary? ParsePathByAddress<TAddress>(TAddress address, PathString path);
}
