// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public sealed class EndpointSelectorContext
    {
        private int _index;

        public EndpointSelectorContext(HttpContext httpContext, DispatcherValueCollection values, IList<Endpoint> endpoints, IList<EndpointSelector> selectors)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (selectors == null)
            {
                throw new ArgumentNullException(nameof(selectors));
            }

            HttpContext = httpContext;
            Values = values;
            Endpoints = endpoints;
            Selectors = selectors; 
        }

        public IList<Endpoint> Endpoints { get; }

        public HttpContext HttpContext { get; }

        public IList<EndpointSelector> Selectors { get; }

        public RequestDelegate ShortCircuit { get; set; }

        public DispatcherValueCollection Values { get; }

        public Task InvokeNextAsync()
        {
            if (_index >= Selectors.Count)
            {
                return Task.CompletedTask;
            }

            var selector = Selectors[_index++];
            return selector.SelectAsync(this);
        }

        public Snapshot CreateSnapshot()
        {
            return new Snapshot(_index, Endpoints);
        }

        public void RestoreSnapshot(Snapshot snapshot)
        {
            snapshot.Apply(this);
        }

        public struct Snapshot
        {
            private readonly int _index;
            private readonly Endpoint[] _endpoints;

            internal Snapshot(int index, IList<Endpoint> endpoints)
            {
                _index = index;
                _endpoints = endpoints.ToArray();
            }

            internal void Apply(EndpointSelectorContext context)
            {
                context._index = _index;

                context.Endpoints.Clear();
                for (var i = 0; i < _endpoints.Length; i++)
                {
                    context.Endpoints.Add(_endpoints[i]);
                }
            }
        }
    }
}
