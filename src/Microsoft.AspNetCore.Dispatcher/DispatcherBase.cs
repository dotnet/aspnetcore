// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public abstract class DispatcherBase
    {
        private IList<Endpoint> _endpoints;
        private IList<EndpointSelector> _endpointSelectors;

        public virtual IList<Endpoint> Endpoints
        {
            get
            {
                if (_endpoints == null)
                {
                    _endpoints = new List<Endpoint>();
                }

                return _endpoints;
            }
        }

        public virtual IList<EndpointSelector> Selectors
        {
            get
            {
                if (_endpointSelectors == null)
                {
                    _endpointSelectors = new List<EndpointSelector>();
                }

                return _endpointSelectors;
            }
        }

        public virtual async Task InvokeAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var feature = httpContext.Features.Get<IDispatcherFeature>();
            if (await TryMatchAsync(httpContext))
            {
                if (feature.RequestDelegate != null)
                {
                    // Short circuit, no need to select an endpoint.
                    return;
                }

                var selectorContext = new EndpointSelectorContext(httpContext, Endpoints.ToList(), Selectors);
                await selectorContext.InvokeNextAsync();

                switch (selectorContext.Endpoints.Count)
                {
                    case 0:
                        break;

                    case 1:
                        
                        feature.Endpoint = selectorContext.Endpoints[0];
                        break;

                    default:
                        throw new InvalidOperationException("Ambiguous bro!");

                }
            }
        }

        protected abstract Task<bool> TryMatchAsync(HttpContext httpContext);
    }
}
