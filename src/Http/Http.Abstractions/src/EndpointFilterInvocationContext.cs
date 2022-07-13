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
}
