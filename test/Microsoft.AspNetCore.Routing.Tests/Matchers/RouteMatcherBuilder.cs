// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class RouteMatcherBuilder : MatcherBuilder
    {
        private readonly RouteCollection _routes = new RouteCollection();
        private readonly IInlineConstraintResolver _constraintResolver;

        public RouteMatcherBuilder()
        {
            _constraintResolver = new DefaultInlineConstraintResolver(Options.Create(new RouteOptions()));
        }

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            var handler = new RouteHandler(c =>
            {
                c.Features.Get<IEndpointFeature>().Endpoint = endpoint;
                return Task.CompletedTask;
            });
            _routes.Add(new Route(handler, endpoint.Template, _constraintResolver));
        }

        public override Matcher Build()
        {
            return new RouteMatcher(_routes);
        }
    }
}
