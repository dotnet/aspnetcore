// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class TreeRouterMatcher : Matcher
    {
        public static MatcherBuilder CreateBuilder() => new Builder();

        private TreeRouter _inner;

        private TreeRouterMatcher(TreeRouter inner)
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
            private readonly TreeRouteBuilder _inner;

            public Builder()
            {
                _inner = new TreeRouteBuilder(
                    NullLoggerFactory.Instance,
                    new DefaultObjectPool<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy()),
                    new DefaultInlineConstraintResolver(Options.Create(new RouteOptions())));
            }

            public override void AddEntry(string template, MatcherEndpoint endpoint)
            {
                var handler = new RouteHandler(c =>
                {
                    c.Features.Get<IEndpointFeature>().Endpoint = endpoint;
                    return Task.CompletedTask;
                });
                _inner.MapInbound(handler, TemplateParser.Parse(template), "default", 0);
            }

            public override Matcher Build()
            {
                return new TreeRouterMatcher(_inner.Build());
            }
        }
    }
}
