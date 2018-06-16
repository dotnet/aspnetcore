// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class TreeRouterMatcherBuilder : MatcherBuilder
    {
        private readonly TreeRouteBuilder _inner;

        public TreeRouterMatcherBuilder()
        {
            _inner = new TreeRouteBuilder(
                NullLoggerFactory.Instance,
                new DefaultObjectPool<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy()),
                new DefaultInlineConstraintResolver(Options.Create(new RouteOptions())));
        }

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            var handler = new RouteHandler(c =>
            {
                var feature = c.Features.Get<IEndpointFeature>();
                feature.Endpoint = endpoint;
                feature.Invoker = MatcherEndpoint.EmptyInvoker;

                return Task.CompletedTask;
            });

            // MatcherEndpoint.Values contains the default values parsed from the template
            // as well as those specified with a literal. We need to separate those
            // for legacy cases.
            var defaults = new RouteValueDictionary(endpoint.Values);
            for (var i = 0; i < endpoint.ParsedTemlate.Parameters.Count; i++)
            {
                var parameter = endpoint.ParsedTemlate.Parameters[i];
                if (parameter.DefaultValue == null && defaults.ContainsKey(parameter.Name))
                {
                    throw new InvalidOperationException(
                        "The TreeRouter does not support non-inline default values.");
                }
            }

            _inner.MapInbound(
                handler, 
                endpoint.ParsedTemlate,
                routeName: null,
                order: endpoint.Order);
        }

        public override Matcher Build()
        {
            return new TreeRouterMatcher(_inner.Build());
        }
    }
}
