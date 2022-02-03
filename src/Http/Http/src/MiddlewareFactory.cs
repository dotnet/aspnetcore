// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Default implementation for <see cref="IMiddlewareFactory"/>.
/// </summary>
public class MiddlewareFactory : IMiddlewareFactory
{
    // The default middleware factory is just an IServiceProvider proxy.
    // This should be registered as a scoped service so that the middleware instances
    // don't end up being singletons.
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="MiddlewareFactory"/>.
    /// </summary>
    /// <param name="serviceProvider">The application services.</param>
    public MiddlewareFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public IMiddleware? Create(Type middlewareType)
    {
        return _serviceProvider.GetRequiredService(middlewareType) as IMiddleware;
    }

    /// <inheritdoc/>
    public void Release(IMiddleware middleware)
    {
        // The container owns the lifetime of the service
    }
}
