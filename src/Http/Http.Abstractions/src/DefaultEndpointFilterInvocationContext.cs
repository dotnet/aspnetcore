// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides a default implementation for wrapping the <see cref="HttpContext"/> and parameters
/// provided to a route handler.
/// </summary>
public sealed class DefaultEndpointFilterInvocationContext : EndpointFilterInvocationContext
{
    /// <summary>
    /// Creates a new instance of the <see cref="DefaultEndpointFilterInvocationContext"/> for a given request.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="arguments">A list of parameters provided in the current request.</param>
    public DefaultEndpointFilterInvocationContext(HttpContext httpContext, params object?[] arguments)
    {
        HttpContext = httpContext;
        Arguments = arguments;
    }

    /// <inheritdoc />
    public override HttpContext HttpContext { get; }

    /// <inheritdoc />
    public override IList<object?> Arguments { get; }

    /// <inheritdoc />
    public override T GetArgument<T>(int index)
    {
        return (T)Arguments[index]!;
    }
}
