// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Performance
{
    public class RoutingBenchmark
    {
        private const int NumberOfRequestTypes = 3;
        private const int Iterations = 100;

        private readonly IRouter _treeRouter;
        private readonly RequestEntry[] _requests;

        public RoutingBenchmark()
        {
            var handler = new RouteHandler((next) => Task.FromResult<object>(null));
 
            var treeBuilder = new TreeRouteBuilder(
                NullLoggerFactory.Instance,
                new DefaultObjectPool<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy()),
                new DefaultInlineConstraintResolver(new OptionsManager<RouteOptions>(new OptionsFactory<RouteOptions>(Enumerable.Empty<IConfigureOptions<RouteOptions>>(), Enumerable.Empty<IPostConfigureOptions<RouteOptions>>()))));

            treeBuilder.MapInbound(handler, TemplateParser.Parse("api/Widgets"), "default", 0);
            treeBuilder.MapInbound(handler, TemplateParser.Parse("api/Widgets/{id}"), "default", 0);
            treeBuilder.MapInbound(handler, TemplateParser.Parse("api/Widgets/search/{term}"), "default", 0);
            treeBuilder.MapInbound(handler, TemplateParser.Parse("admin/users/{id}"), "default", 0);
            treeBuilder.MapInbound(handler, TemplateParser.Parse("admin/users/{id}/manage"), "default", 0);

            _treeRouter = treeBuilder.Build();

            _requests = new RequestEntry[NumberOfRequestTypes];

            _requests[0].HttpContext = new DefaultHttpContext();
            _requests[0].HttpContext.Request.Path = "/api/Widgets/5";
            _requests[0].IsMatch = true;
            _requests[0].Values = new RouteValueDictionary(new { id = 5 });

            _requests[1].HttpContext = new DefaultHttpContext();
            _requests[1].HttpContext.Request.Path = "/admin/users/17/mAnage";
            _requests[1].IsMatch = true;
            _requests[1].Values = new RouteValueDictionary(new { id = 17 });

            _requests[2].HttpContext = new DefaultHttpContext();
            _requests[2].HttpContext.Request.Path = "/api/Widgets/search/dldldldldld/ddld";
            _requests[2].IsMatch = false;
            _requests[2].Values = new RouteValueDictionary();
        }

        [Benchmark(Description = "Attribute Routing", OperationsPerInvoke = Iterations * NumberOfRequestTypes)]
        public async Task AttributeRouting()
        {
            for (var i = 0; i < Iterations; i++)
            {
                for (var j = 0; j < _requests.Length; j++)
                {
                    var context = new RouteContext(_requests[j].HttpContext);

                    await _treeRouter.RouteAsync(context);

                    Verify(context, j);
                }
            }
        }

        private void Verify(RouteContext context, int i)
        {
            if (_requests[i].IsMatch)
            {
                if (context.Handler == null)
                {
                    throw new InvalidOperationException($"Failed {i}");
                }

                var values = _requests[i].Values;
                if (values.Count != context.RouteData.Values.Count)
                {
                    throw new InvalidOperationException($"Failed {i}");
                }
            }
            else
            {
                if (context.Handler != null)
                {
                    throw new InvalidOperationException($"Failed {i}");
                }

                if (context.RouteData.Values.Count != 0)
                {
                    throw new InvalidOperationException($"Failed {i}");
                }
            }
        }

        private struct RequestEntry
        {
            public HttpContext HttpContext;
            public bool IsMatch;
            public RouteValueDictionary Values;
        }
    }
}