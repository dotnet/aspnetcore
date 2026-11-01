// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

// Measures view lookups that hit the view location cache, as happens for every view, layout and partial
// rendered by an app after warm up.
[MemoryDiagnoser]
public class ViewEngineBenchmark
{
    private ServiceProvider _services;
    private IRazorViewEngine _viewEngine;
    private ActionContext _actionContext;

    [GlobalSetup]
    public void Setup()
    {
        var viewsAssembly = typeof(ViewAssemblyMarker).Assembly;
        var partManager = new ApplicationPartManager();
        partManager.ApplicationParts.Add(CompiledRazorAssemblyApplicationPartFactory.GetDefaultApplicationParts(viewsAssembly).Single());
        var listener = new DiagnosticListener(nameof(ViewEngineBenchmark));

        _services = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton<DiagnosticSource>(listener)
            .AddSingleton(listener)
            .AddSingleton<IWebHostEnvironment, HostingEnvironment>()
            .AddSingleton(partManager)
            .AddMvc()
            .Services
            .BuildServiceProvider();

        _viewEngine = _services.GetRequiredService<IRazorViewEngine>();

        var routeData = new RouteData();
        routeData.Values["controller"] = "Home";
        routeData.Values["action"] = "Index";
        var actionDescriptor = new ActionDescriptor();
        actionDescriptor.RouteValues["controller"] = "Home";
        actionDescriptor.RouteValues["action"] = "Index";
        _actionContext = new ActionContext(new DefaultHttpContext(), routeData, actionDescriptor);

        // Populate the cache.
        FindPartial().EnsureSuccessful(originalLocations: null);
    }

    [GlobalCleanup]
    public void Cleanup() => _services.Dispose();

    [Benchmark]
    public ViewEngineResult FindPartial() => _viewEngine.FindView(_actionContext, "_ViewEngineBenchmarkPartial", isMainPage: false);

    private sealed class HostingEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = typeof(ViewEngineBenchmark).Assembly.GetName().Name;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Production";
    }
}
