// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// Provides an abstraction for dynamically manipulating route value to select a controller action or page.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DynamicRouteValueTransformer"/> can be used with
/// <see cref="Microsoft.AspNetCore.Builder.ControllerEndpointRouteBuilderExtensions.MapDynamicControllerRoute{TTransformer}(IEndpointRouteBuilder, string)" />
/// or <c>MapDynamicPageRoute</c> to implement custom logic that selects a controller action or page.
/// </para>
/// <para>
/// The route values returned from a <see cref="TransformAsync(HttpContext, RouteValueDictionary)"/> implementation
/// will be used to select an action based on matching of the route values. All actions that match the route values
/// will be considered as candidates, and may be further disambiguated by
/// <see cref="FilterAsync(HttpContext, RouteValueDictionary, IReadOnlyList{Endpoint})" /> as well as
/// <see cref="IEndpointSelectorPolicy" /> implementations such as <see cref="HttpMethodMatcherPolicy" />.
/// </para>
/// <para>
/// Operations on a <see cref="DynamicRouteValueTransformer" /> instance will be called for each dynamic endpoint
/// in the following sequence:
///
/// <list type="bullet">
///   <item><description><see cref="State" /> is set</description></item>
///   <item><description><see cref="TransformAsync(HttpContext, RouteValueDictionary)"/></description></item>
///   <item><description><see cref="FilterAsync(HttpContext, RouteValueDictionary, IReadOnlyList{Endpoint})" /></description></item>
/// </list>
///
/// Implementations that are registered with the service collection as transient may safely use class
/// members to persist state across these operations.
/// </para>
/// <para>
/// Implementations <see cref="DynamicRouteValueTransformer" /> should be registered with the service
/// collection as type <see cref="DynamicRouteValueTransformer" />. Implementations can use any service
/// lifetime. Implementations that make use of <see cref="State" /> must be registered as transient.
/// </para>
/// </remarks>
public abstract class DynamicRouteValueTransformer
{
    /// <summary>
    /// Gets or sets a state value. An arbitrary value passed to the transformer from where it was registered.
    /// </summary>
    /// <remarks>
    /// Implementations that make use of <see cref="State" /> must be registered as transient with the service
    /// collection.
    /// </remarks>
    public object? State { get; set; }

    /// <summary>
    /// Creates a set of transformed route values that will be used to select an action.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext" /> associated with the current request.</param>
    /// <param name="values">The route values associated with the current match. Implementations should not modify <paramref name="values"/>.</param>
    /// <returns>Returns a set of new route values, else null to indicate there was no match.</returns>
    public abstract ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values);

    /// <summary>
    /// Filters the set of endpoints that were chosen as a result of lookup based on the route values returned by
    /// <see cref="TransformAsync(HttpContext, RouteValueDictionary)" />.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext" /> associated with the current request.</param>
    /// <param name="values">The route values returned from <see cref="TransformAsync(HttpContext, RouteValueDictionary)" />.</param>
    /// <param name="endpoints">
    /// The endpoints that were chosen as a result of lookup based on the route values returned by
    /// <see cref="TransformAsync(HttpContext, RouteValueDictionary)" />.
    /// </param>
    /// <returns>Asynchronously returns a list of endpoints to apply to the matches collection.</returns>
    /// <remarks>
    /// <para>
    /// Implementations of <see cref="FilterAsync(HttpContext, RouteValueDictionary, IReadOnlyList{Endpoint})" /> may further
    /// refine the list of endpoints chosen based on route value matching by returning a new list of endpoints based on
    /// <paramref name="endpoints" />.
    /// </para>
    /// <para>
    /// <see cref="FilterAsync(HttpContext, RouteValueDictionary, IReadOnlyList{Endpoint})" /> will not be called in the case
    /// where zero endpoints were matched based on route values.
    /// </para>
    /// </remarks>
    public virtual ValueTask<IReadOnlyList<Endpoint>> FilterAsync(HttpContext httpContext, RouteValueDictionary values, IReadOnlyList<Endpoint> endpoints)
    {
        return new ValueTask<IReadOnlyList<Endpoint>>(endpoints);
    }
}
