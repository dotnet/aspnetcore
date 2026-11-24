// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Performance;

public class PageHandlerSelectorBenchmark
{
    private IPageHandlerMethodSelector _selector;
    private PageContext _getContext;
    private PageContext _namedPostContext;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvc();
        _selector = services.BuildServiceProvider().GetRequiredService<IPageHandlerMethodSelector>();

        var handlers = new List<HandlerMethodDescriptor>
        {
            new HandlerMethodDescriptor { HttpMethod = "GET" },
            new HandlerMethodDescriptor { HttpMethod = "POST" },
            new HandlerMethodDescriptor { HttpMethod = "POST", Name = "Delete" },
        };

        _getContext = CreateContext(handlers, "GET", handlerName: null);
        _namedPostContext = CreateContext(handlers, "POST", handlerName: "Delete");
    }

    [Benchmark]
    public object SelectGet() => _selector.Select(_getContext);

    [Benchmark]
    public object SelectNamedPost() => _selector.Select(_namedPostContext);

    private static PageContext CreateContext(IList<HandlerMethodDescriptor> handlers, string method, string handlerName)
    {
        var routeData = new RouteData();
        if (handlerName != null)
        {
            routeData.Values["handler"] = handlerName;
        }

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;

        return new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor { HandlerMethods = handlers },
            RouteData = routeData,
            HttpContext = httpContext,
        };
    }
}
