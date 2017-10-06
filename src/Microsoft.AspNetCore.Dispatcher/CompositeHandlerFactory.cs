// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class CompositeHandlerFactory : IHandlerFactory
    {
        private readonly IHandlerFactory[] _factories;

        public CompositeHandlerFactory(IEnumerable<IHandlerFactory> factories)
        {
            if (factories == null)
            {
                throw new ArgumentNullException(nameof(factories));
            }

            _factories = factories.ToArray();
        }

        public Func<RequestDelegate, RequestDelegate> CreateHandler(Endpoint endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            for (var i = 0; i < _factories.Length; i++)
            {
                var handler = _factories[i].CreateHandler(endpoint);
                if (handler != null)
                {
                    return handler;
                }
            }

            return null;
        }
    }
}
