// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides methods to create middleware.
/// </summary>
public interface IMiddlewareFactory
{
    /// <summary>
    /// Creates a middleware instance for each request.
    /// </summary>
    /// <param name="middlewareType">The concrete <see cref="Type"/> of the <see cref="IMiddleware"/>.</param>
    /// <returns>The <see cref="IMiddleware"/> instance.</returns>
    IMiddleware? Create(Type middlewareType);

    /// <summary>
    /// Releases a <see cref="IMiddleware"/> instance at the end of each request.
    /// </summary>
    /// <param name="middleware">The <see cref="IMiddleware"/> instance to release.</param>
    void Release(IMiddleware middleware);
}
