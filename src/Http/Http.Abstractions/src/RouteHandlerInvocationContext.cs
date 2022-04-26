// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides an abstraction for wrapping the <see cref="HttpContext"/> and parameters
/// provided to a route handler.
/// </summary>
public class RouteHandlerInvocationContext
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // Provide an internal parameterless constructor for RHIC so that child classes
    // can instantiate their instances without having to instantiate this base class
    // and allocate an object[]
    internal RouteHandlerInvocationContext() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// Creates a new instance of the <see cref="RouteHandlerInvocationContext"/> for a given request.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="parameters">A list of parameters provided in the current request.</param>
    public RouteHandlerInvocationContext(HttpContext httpContext, params object[] parameters)
    {
        HttpContext = httpContext;
        Parameters = parameters;
    }

    /// <summary>
    /// The <see cref="HttpContext"/> associated with the current request being processed by the filter.
    /// </summary>
    public virtual HttpContext HttpContext { get; }

    /// <summary>
    /// A list of parameters provided in the current request to the filter.
    /// <remarks>
    /// This list is not read-only to permit modifying of existing parameters by filters.
    /// </remarks>
    /// </summary>
    public virtual IList<object?> Parameters { get; }

    /// <summary>
    /// Retrieve the parameter given its position in the argument list.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of the resolved parameter.</typeparam>
    /// <param name="index">An integer representing the position of the parameter in the argument list.</param>
    /// <returns>The parameter at a given <paramref name="index"/></returns>
    public virtual T GetParameter<T>(int index)
    {
        return (T)Parameters[index]!;
    }
}
