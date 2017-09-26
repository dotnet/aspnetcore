// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Dispatcher
{
    public abstract class DispatcherBase : IAddressCollectionProvider, IEndpointCollectionProvider
    {
        private List<Address> _addresses;
        private List<Endpoint> _endpoints;
        private List<EndpointSelector> _endpointSelectors;

        public virtual IList<Address> Addresses
        {
            get
            {
                if (_addresses == null)
                {
                    _addresses = new List<Address>();
                }

                return _addresses;
            }
        }

        public virtual DispatcherDataSource DataSource { get; set; }

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

        public IChangeToken ChangeToken => DataSource?.ChangeToken ?? NullChangeToken.Singleton;

        IReadOnlyList<Address> IAddressCollectionProvider.Addresses => GetAddresses();

        IReadOnlyList<Endpoint> IEndpointCollectionProvider.Endpoints => GetEndpoints();

        protected virtual IReadOnlyList<Address> GetAddresses()
        {
            return ((IAddressCollectionProvider)DataSource)?.Addresses ?? _addresses ?? (IReadOnlyList<Address>)Array.Empty<Address>();
        }

        protected virtual IReadOnlyList<Endpoint> GetEndpoints()
        {
            return ((IEndpointCollectionProvider)DataSource)?.Endpoints ?? _endpoints ?? (IReadOnlyList<Endpoint>)Array.Empty<Endpoint>();
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

                feature.Endpoint = await SelectEndpointAsync(httpContext, GetEndpoints(), Selectors);
            }
        }

        protected virtual Task<bool> TryMatchAsync(HttpContext httpContext)
        {
            // By default don't apply any criteria.
            return Task.FromResult(true);
        }

        protected virtual async Task<Endpoint> SelectEndpointAsync(HttpContext httpContext, IEnumerable<Endpoint> endpoints, IEnumerable<EndpointSelector> selectors)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (selectors == null)
            {
                throw new ArgumentNullException(nameof(selectors));
            }

            var selectorContext = new EndpointSelectorContext(httpContext, endpoints.ToList(), selectors.ToList());
            await selectorContext.InvokeNextAsync();

            switch (selectorContext.Endpoints.Count)
            {
                case 0:
                    return null;

                case 1:
                    return selectorContext.Endpoints[0];

                default:
                    throw new InvalidOperationException("Ambiguous bro!");

            }
        }
    }
}
