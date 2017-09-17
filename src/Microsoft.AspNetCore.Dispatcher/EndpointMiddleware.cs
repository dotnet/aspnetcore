// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class EndpointMiddleware
    {
        private readonly DispatcherOptions _options;
        private RequestDelegate _next;

        public EndpointMiddleware(IOptions<DispatcherOptions> options, RequestDelegate next)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _options = options.Value;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var feature = context.Features.Get<IDispatcherFeature>();
            if (feature.Endpoint != null && feature.RequestDelegate == null)
            {
                for (var i = 0; i < _options.HandlerFactories.Count; i++)
                {
                    var handler = _options.HandlerFactories[i](feature.Endpoint);
                    if (handler != null)
                    {
                        feature.RequestDelegate = handler(_next);
                        break;
                    }
                }
            }

            if (feature.RequestDelegate != null)
            {
                await feature.RequestDelegate(context);
            }

            await _next(context);
        }
    }
}
