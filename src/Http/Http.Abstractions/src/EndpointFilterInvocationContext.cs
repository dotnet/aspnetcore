// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides an abstraction for wrapping the <see cref="HttpContext"/> and arguments
/// provided to a route handler.
/// </summary>
public abstract class EndpointFilterInvocationContext
{
    /// <summary>
    /// The <see cref="HttpContext"/> associated with the current request being processed by the filter.
    /// </summary>
    public abstract HttpContext HttpContext { get; }

    /// <summary>
    /// A list of arguments provided in the current request to the filter.
    /// <remarks>
    /// This list is not read-only to permit modifying of existing arguments by filters.
    /// </remarks>
    /// </summary>
    public abstract IList<object?> Arguments { get; }

    /// <summary>
    /// Retrieve the argument given its position in the argument list.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of the resolved argument.</typeparam>
    /// <param name="index">An integer representing the position of the argument in the argument list.</param>
    /// <returns>The argument at a given <paramref name="index"/>.</returns>
    public abstract T GetArgument<T>(int index);

    /// <summary>
    /// Creates the default implementation of a <see cref="EndpointFilterInvocationContext"/>.    
    /// </summary>
    public static EndpointFilterInvocationContext Create(HttpContext httpContext) =>
        new DefaultEndpointFilterInvocationContext(httpContext);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext Create<T>(HttpContext httpContext, T arg) =>
        new EndpointFilterInvocationContext<T>(httpContext, arg);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext Create<T1, T2>(HttpContext httpContext, T1 arg1, T2 arg2) =>
        new EndpointFilterInvocationContext<T1, T2>(httpContext, arg1, arg2);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext Create<T1, T2, T3>(HttpContext httpContext, T1 arg1, T2 arg2, T3 arg3) =>
        new EndpointFilterInvocationContext<T1, T2, T3>(httpContext, arg1, arg2, arg3);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext Create<T1, T2, T3, T4>(HttpContext httpContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
        new EndpointFilterInvocationContext<T1, T2, T3, T4>(httpContext, arg1, arg2, arg3, arg4);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext Create<T1, T2, T3, T4, T5>(HttpContext httpContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
        new EndpointFilterInvocationContext<T1, T2, T3, T4, T5>(httpContext, arg1, arg2, arg3, arg4, arg5);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext Create<T1, T2, T3, T4, T5, T6>(HttpContext httpContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
        new EndpointFilterInvocationContext<T1, T2, T3, T4, T5, T6>(httpContext, arg1, arg2, arg3, arg4, arg5, arg6);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext Create<T1, T2, T3, T4, T5, T6, T7>(HttpContext httpContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) =>
        new EndpointFilterInvocationContext<T1, T2, T3, T4, T5, T6, T7>(httpContext, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext Create<T1, T2, T3, T4, T5, T6, T7, T8>(HttpContext httpContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) =>
        new EndpointFilterInvocationContext<T1, T2, T3, T4, T5, T6, T7, T8>(httpContext, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
}
