// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class TreeRouterMatcherConformanceTest : MatcherConformanceTest
    {
        internal override Matcher CreateMatcher(MatcherEndpoint endpoint)
        {
            var builder = new TreeRouteBuilder(
                NullLoggerFactory.Instance,
                new DefaultObjectPool<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy()),
                new DefaultInlineConstraintResolver(Options.Create(new RouteOptions())));

            var handler = new RouteHandler(c =>
            {
                var feature = c.Features.Get<IEndpointFeature>();
                feature.Endpoint = endpoint;
                feature.Invoker = MatcherEndpoint.EmptyInvoker;

                return Task.CompletedTask;
            });

            builder.MapInbound(handler, TemplateParser.Parse(endpoint.Template), "default", 0);

            return new TreeRouterMatcher(builder.Build());
        }
    }
}
