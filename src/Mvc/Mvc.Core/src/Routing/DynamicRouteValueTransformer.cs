// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Mvc.Routing
{
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
    /// will be considered as candidates, and may be further disambiguated by <see cref="IEndpointSelectorPolicy" />
    /// implementations such as <see cref="HttpMethodMatcherPolicy" />.
    /// </para>
    /// <para>
    /// Implementations <see cref="DynamicRouteValueTransformer" /> should be registered with the service
    /// collection as type <see cref="DynamicRouteValueTransformer" />. Implementations can use any service
    /// lifetime.
    /// </para>
    /// </remarks>
    public abstract class DynamicRouteValueTransformer
    {
        /// <summary>
        /// Creates a set of transformed route values that will be used to select an action.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext" /> associated with the current request.</param>
        /// <param name="values">The route values associated with the current match. Implementations should not modify <paramref name="values"/>.</param>
        /// <returns>A task which asynchronously returns a set of route values.</returns>
        public abstract ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values);
    }
}
