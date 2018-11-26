// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing
{
    internal class ModelEndpointDataSource : EndpointDataSource
    {
        private List<EndpointConventionBuilder> _endpointConventionBuilders;

        public ModelEndpointDataSource()
        {
            _endpointConventionBuilders = new List<EndpointConventionBuilder>();
        }

        public IEndpointConventionBuilder AddEndpointModel(EndpointModel endpointModel)
        {
            var builder = new EndpointConventionBuilder(endpointModel);
            _endpointConventionBuilders.Add(builder);

            return builder;
        }

        public override IChangeToken GetChangeToken()
        {
            return NullChangeToken.Singleton;
        }

        public override IReadOnlyList<Endpoint> Endpoints => _endpointConventionBuilders.Select(e => e.Build()).ToArray();

        // for testing
        internal IEnumerable<EndpointModel> EndpointModels => _endpointConventionBuilders.Select(b => b.EndpointModel);

        private class EndpointConventionBuilder : IEndpointConventionBuilder
        {
            internal EndpointModel EndpointModel { get; }

            private readonly List<Action<EndpointModel>> _conventions;

            public EndpointConventionBuilder(EndpointModel endpointModel)
            {
                EndpointModel = endpointModel;
                _conventions = new List<Action<EndpointModel>>();
            }

            public void Apply(Action<EndpointModel> convention)
            {
                _conventions.Add(convention);
            }

            public Endpoint Build()
            {
                foreach (var convention in _conventions)
                {
                    convention(EndpointModel);
                }

                return EndpointModel.Build();
            }
        }
    }
}