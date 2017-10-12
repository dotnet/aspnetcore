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
    public abstract class MatcherBase : IMatcher, IAddressCollectionProvider, IEndpointCollectionProvider
    {
        private List<Address> _addresses;
        private List<Endpoint> _endpoints;
        private List<EndpointSelector> _endpointSelectors;

        private object _lock;
        private bool _servicesInitialized;
        private bool _selectorsInitialized;
        private readonly Func<object> _selectorInitializer;

        public MatcherBase()
        {
            _lock = new object();
            _selectorInitializer = InitializeSelectors;
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

        public virtual async Task MatchAsync(MatcherContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            EnsureServicesInitialized(context);

            context.Values = await MatchRequestAsync(context.HttpContext);
            if (context.Values != null)
            {
                await SelectEndpointAsync(context, GetEndpoints());
            }
        }

        protected virtual Task<DispatcherValueCollection> MatchRequestAsync(HttpContext httpContext)
        {
            // By default don't apply any criteria or provide any values.
            return Task.FromResult(new DispatcherValueCollection());
        }

        protected virtual void InitializeServices(IServiceProvider services)
        {
        }

        protected virtual async Task SelectEndpointAsync(MatcherContext context, IEnumerable<Endpoint> endpoints)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            EnsureSelectorsInitialized();

            var selectorContext = new EndpointSelectorContext(context.HttpContext, context.Values, endpoints.ToList(), Selectors.ToList());
            await selectorContext.InvokeNextAsync();

            if (selectorContext.ShortCircuit != null)
            {
                context.ShortCircuit = selectorContext.ShortCircuit;
                Logger.RequestShortCircuitedMatcherBase(context);
                return;
            }

            switch (selectorContext.Endpoints.Count)
            {
                case 0:
                    Logger.NoEndpointsMatched(context.HttpContext.Request.Path);
                    return;

                case 1:
                    context.Endpoint = selectorContext.Endpoints[0];

                    Logger.EndpointMatchedMatcherBase(context.Endpoint);
                    return;

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

        protected void EnsureSelectorsInitialized()
        {
            object _ = null;
            LazyInitializer.EnsureInitialized(ref _, ref _selectorsInitialized, ref _lock, _selectorInitializer);
        }

        private object InitializeSelectors()
        {
            foreach (var selector in Selectors)
            {
                selector.Initialize(this);
            }

            return null;
        }

        protected void EnsureServicesInitialized(MatcherContext context)
        {
            if (Volatile.Read(ref _servicesInitialized))
            {
                return;
            }

            EnsureServicesInitializedSlow(context);
        }

        private void EnsureServicesInitializedSlow(MatcherContext context)
        {
            lock (_lock)
            {
                if (!Volatile.Read(ref _servicesInitialized))
                {
                    var services = context.HttpContext.RequestServices;
                    Logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
                    InitializeServices(services);
                }
            }
        }
    }
}
