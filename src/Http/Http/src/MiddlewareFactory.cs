// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http
{
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
}
