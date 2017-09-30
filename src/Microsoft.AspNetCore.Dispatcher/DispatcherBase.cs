// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Dispatcher
{
    public abstract class DispatcherBase : IAddressCollectionProvider, IEndpointCollectionProvider
    {
        private List<Address> _addresses;
        private List<Endpoint> _endpoints;
        private List<EndpointSelector> _endpointSelectors;

        private object _initialize;
        private bool _selectorsInitialized;
        private readonly Func<object> _initializer;
        private object _lock;

        private bool _servicesInitialized;

        public DispatcherBase()
        {
            _lock = new object();
            _initializer = InitializeSelectors;
        }

        protected ILogger Logger { get; private set; }

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

            EnsureServicesInitialized(httpContext);

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

            LazyInitializer.EnsureInitialized(ref _initialize, ref _selectorsInitialized, ref _lock, _initializer);

            var selectorContext = new EndpointSelectorContext(httpContext, endpoints.ToList(), selectors.ToList());
            await selectorContext.InvokeNextAsync();

            switch (selectorContext.Endpoints.Count)
            {
                case 0:
                    Logger.NoEndpointsMatched(httpContext.Request.Path);
                    return null;

                case 1:
                    Logger.EndpointMatched(selectorContext.Endpoints[0].DisplayName);
                    return selectorContext.Endpoints[0];

                default:
                    var endpointNames = string.Join(
                            Environment.NewLine,
                            selectorContext.Endpoints.Select(a => a.DisplayName));

                    Logger.AmbiguousEndpoints(endpointNames);

                    var message = Resources.FormatAmbiguousEndpoints(
                        Environment.NewLine,
                        endpointNames);

                    throw new AmbiguousEndpointException(message);
            }
        }

        private object InitializeSelectors()
        {
            foreach (var selector in Selectors)
            {
                selector.Initialize(this);
            }

            return null;
        }

        protected void EnsureServicesInitialized(HttpContext httpContext)
        {
            if (Volatile.Read(ref _servicesInitialized))
            {
                return;
            }

            EnsureServicesInitializedSlow(httpContext);
        }

        private void EnsureServicesInitializedSlow(HttpContext httpContext)
        {
            lock (_lock)
            {
                if (!Volatile.Read(ref _servicesInitialized))
                {
                    Logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
                }
            }
        }
    }
}
