// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http
{
    public class MiddlewareFactory : IMiddlewareFactory
    {
        // The default middleware factory is just an IServiceProvider proxy.
        // This should be registered as a scoped service so that the middleware instances
        // don't end up being singletons.
        private readonly IServiceProvider _serviceProvider;

        public MiddlewareFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IMiddleware Create(Type middlewareType)
        {
            return _serviceProvider.GetRequiredService(middlewareType) as IMiddleware;
        }

        public void Release(IMiddleware middleware)
        {
            // The container owns the lifetime of the service
        }
    }
}
