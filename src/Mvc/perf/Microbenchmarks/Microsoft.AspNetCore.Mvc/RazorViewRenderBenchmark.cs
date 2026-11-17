// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

// Renders a compiled Razor view end to end per operation: resolve the view through the view engine,
// create the view data and execute it, including the partial views it renders. Unlike
// HelperPerformanceBenchmark this does not use [IterationSetup], so each iteration runs many
// invocations and time and allocations are stable enough for A/B comparisons.
public class RazorViewRenderBenchmark
{
    private ServiceProvider _serviceProvider;
    private ICompositeViewEngine _viewEngine;
    private ITempDataDictionaryFactory _tempDataDictionaryFactory;
    private IModelMetadataProvider _metadataProvider;
    private readonly StringBuilder _output = new();

    [Params("HelperPartialAsync", "HelperPartialTagHelper", "HelperTyped")]
    public string View { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var viewsAssembly = typeof(ViewAssemblyMarker).Assembly;
        var services = new ServiceCollection();
        var listener = new DiagnosticListener(GetType().Assembly.FullName);
        var partManager = new ApplicationPartManager();
        partManager.ApplicationParts.Add(CompiledRazorAssemblyApplicationPartFactory.GetDefaultApplicationParts(viewsAssembly).Single());
        services
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
            .AddSingleton<DiagnosticSource>(listener)
            .AddSingleton(listener)
            .AddSingleton<IWebHostEnvironment, BenchmarkHostingEnvironment>()
            .AddSingleton(partManager)
            .AddScoped<BenchmarkViewExecutor>()
            .AddMvc();

        _serviceProvider = services.BuildServiceProvider();
        _viewEngine = _serviceProvider.GetRequiredService<ICompositeViewEngine>();
        _tempDataDictionaryFactory = _serviceProvider.GetRequiredService<ITempDataDictionaryFactory>();
        _metadataProvider = _serviceProvider.GetRequiredService<IModelMetadataProvider>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _serviceProvider.Dispose();
    }

    [Benchmark]
    public async Task<int> RenderView()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var viewEngineResult = _viewEngine.GetView(executingFilePath: null, $"~/Views/{View}.cshtml", isMainPage: true);
        viewEngineResult.EnsureSuccessful(originalLocations: null);

        var actionContext = new ActionContext(
            new DefaultHttpContext { RequestServices = scope.ServiceProvider },
            new RouteData(),
            new ActionDescriptor());
        var viewData = new ViewDataDictionary(_metadataProvider, actionContext.ModelState)
        {
            Model = "Hello",
        };
        var tempData = _tempDataDictionaryFactory.GetTempData(actionContext.HttpContext);

        _output.Clear();
        var executor = scope.ServiceProvider.GetRequiredService<BenchmarkViewExecutor>();
        await executor.ExecuteAsync(actionContext, viewEngineResult.View, viewData, tempData, _output);

        if (viewEngineResult.View is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return _output.Length;
    }

    private sealed class BenchmarkViewExecutor : ViewExecutor
    {
        public BenchmarkViewExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticListener diagnosticListener, IModelMetadataProvider modelMetadataProvider)
            : base(viewOptions, writerFactory, viewEngine, tempDataFactory, diagnosticListener, modelMetadataProvider)
        {
        }

        public async Task ExecuteAsync(ActionContext actionContext, IView view, ViewDataDictionary viewData, ITempDataDictionary tempData, StringBuilder output)
        {
            using var writer = new StringWriter(output);
            var viewContext = new ViewContext(actionContext, view, viewData, tempData, writer, ViewOptions.HtmlHelperOptions);
            await ExecuteAsync(viewContext, "text/html", 200);
            await writer.FlushAsync();
        }
    }

    private sealed class BenchmarkHostingEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = typeof(ViewAssemblyMarker).Assembly.FullName;
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
