// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Dispatcher.Performance
{
    public class DispatcherBenchmark
    {
        private const int NumberOfRequestTypes = 3;
        private const int Iterations = 100;

        private readonly IMatcher _treeMatcher;
        private readonly RequestEntry[] _requests;

        public DispatcherBenchmark()
        {
            var dataSource = new DefaultDispatcherDataSource()
            {
                Endpoints =
                {
                    new RoutePatternEndpoint("api/Widgets", Benchmark_Delegate),
                    new RoutePatternEndpoint("api/Widgets/{id}", Benchmark_Delegate),
                    new RoutePatternEndpoint("api/Widgets/search/{term}", Benchmark_Delegate),
                    new RoutePatternEndpoint("admin/users/{id}", Benchmark_Delegate),
                    new RoutePatternEndpoint("admin/users/{id}/manage", Benchmark_Delegate),
                },
            };

            var factory = new TreeMatcherFactory();
            _treeMatcher = factory.CreateMatcher(dataSource, new List<EndpointSelector>());

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
                    var context = new MatcherContext(_requests[j].HttpContext);

                    await _treeMatcher.MatchAsync(context);

                    Verify(context, j);
                }
            }
        }

        private void Verify(MatcherContext context, int i)
        {
            if (_requests[i].IsMatch)
            {
                if (context.Endpoint == null)
                {
                    throw new InvalidOperationException($"Failed {i}");
                }

                var values = _requests[i].Values;
                if (values.Count != context.Values.Count)
                {
                    throw new InvalidOperationException($"Failed {i}");
                }
            }
            else
            {
                if (context.Endpoint != null)
                {
                    throw new InvalidOperationException($"Failed {i}");
                }

                if (context.Values.Count != 0)
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

        private static Task Benchmark_Delegate(HttpContext httpContext)
        {
            return Task.CompletedTask;
        }
    }
}
