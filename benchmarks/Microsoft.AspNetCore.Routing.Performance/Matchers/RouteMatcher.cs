// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class RouteMatcher : Matcher
    {
        public static MatcherBuilder CreateBuilder() => new Builder();

        private IRouter _inner;

        private RouteMatcher(IRouter inner)
        {
            _inner = inner;
        }

        public async override Task MatchAsync(HttpContext httpContext, IEndpointFeature feature)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }
            
            var context = new RouteContext(httpContext);
            await _inner.RouteAsync(context);
            
            if (context.Handler != null)
            {
                httpContext.Features.Set<IEndpointFeature>(feature);
                await context.Handler(httpContext);
            }
        }

        private class Builder : MatcherBuilder
        {
            private readonly RouteCollection _routes = new RouteCollection();
            private readonly IInlineConstraintResolver _constraintResolver;

            public Builder()
            {
                _constraintResolver = new DefaultInlineConstraintResolver(Options.Create(new RouteOptions()));
            }

            public override void AddEntry(string pattern, MatcherEndpoint endpoint)
            {
                var handler = new RouteHandler(c =>
                {
                    c.Features.Get<IEndpointFeature>().Endpoint = endpoint;
                    return Task.CompletedTask;
                });
                _routes.Add(new Route(handler, pattern, _constraintResolver));
            }

            public override Matcher Build()
            {
                return new RouteMatcher(_routes);
            }
        }
    }
}
