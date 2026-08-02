// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !COMPONENTS
using Microsoft.AspNetCore.Http;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif

namespace Microsoft.AspNetCore.Routing;

#if !COMPONENTS
/// <summary>
/// Defines the contract that a class must implement in order to check whether a URL parameter
/// value is valid for a constraint.
/// </summary>
public interface IRouteConstraint : IParameterPolicy
#else
internal interface IRouteConstraint : IParameterPolicy
#endif
{
#if !COMPONENTS
    /// <summary>
    /// Determines whether the URL parameter contains a valid value for this constraint.
    /// </summary>
    /// <param name="httpContext">An object that encapsulates information about the HTTP request.</param>
    /// <param name="route">The router that this constraint belongs to.</param>
    /// <param name="routeKey">The name of the parameter that is being checked.</param>
    /// <param name="values">A dictionary that contains the parameters for the URL.</param>
    /// <param name="routeDirection">
    /// An object that indicates whether the constraint check is being performed
    /// when an incoming request is being handled or when a URL is being generated.
    /// </param>
    /// <returns><c>true</c> if the URL parameter contains a valid value; otherwise, <c>false</c>.</returns>
    bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection);
#else
    bool Match(
        string routeKey,
        RouteValueDictionary values);
#endif
}
