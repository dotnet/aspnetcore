// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Performance;

public class RouteTableFactoryBenchmarks
{
    private const int NumRoutes = 3000;

    private IServiceProvider _serviceProvider;
    private Dictionary<Type, string[]> _templatesByHandler;

    public RouteTableFactoryBenchmarks()
    {
        _serviceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        _templatesByHandler = new Dictionary<Type, string[]>
        {
            { typeof(object), GenerateRoutes() }
        };
    }

    private string[] GenerateRoutes()
    {
        ReadOnlySpan<string> segments1 = ["first", "second", "third", "fourth", "fifth", "sixth", "{parameter1}"];
        ReadOnlySpan<string> segments2 = ["apple", "banana", "cake", "dates", "eggs", "flour", "{parameter2}"];
        ReadOnlySpan<string> segments3 = ["one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "{parameter3}"];
        ReadOnlySpan<string> segments4 = ["create", "read", "update", "delete", "list"];

        var routes = new string[NumRoutes];
        var i = 0;

        foreach (var segment1 in segments1)
        {
            if (i >= routes.Length)
            {
                break;
            }
            routes[i++] = $"/{segment1}";

            foreach (var segment2 in segments2)
            {
                if (i >= routes.Length)
                {
                    break;
                }

                routes[i++] = $"/{segment1}/{segment2}";

                foreach (var segment3 in segments3)
                {
                    if (i >= routes.Length)
                    {
                        break;
                    }

                    routes[i++] = $"/{segment1}/{segment2}/{segment3}";

                    foreach (var segment4 in segments4)
                    {
                        if (i >= routes.Length)
                        {
                            break;
                        }

                        routes[i++] = $"/{segment1}/{segment2}/{segment3}/{segment4}";
                    }
                }
            }
        }

        return routes;
    }

    [Benchmark]
    public object CreateRouteTable() => RouteTableFactory.Create(_templatesByHandler, _serviceProvider);

}
