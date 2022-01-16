// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
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
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

public class RuntimePerformanceBenchmarkBase
{
    private class NullLoggerFactory : ILoggerFactory, ILogger
    {
        void ILoggerFactory.AddProvider(ILoggerProvider provider) { }
        ILogger ILoggerFactory.CreateLogger(string categoryName) => this;
        void IDisposable.Dispose() { }
        IDisposable ILogger.BeginScope<TState>(TState state) => null;
        bool ILogger.IsEnabled(LogLevel logLevel) => false;
        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
    }

    private class BenchmarkViewExecutor : ViewExecutor
    {
        public BenchmarkViewExecutor(IOptions<MvcViewOptions> viewOptions, IHttpResponseStreamWriterFactory writerFactory, ICompositeViewEngine viewEngine, ITempDataDictionaryFactory tempDataFactory, DiagnosticListener diagnosticListener, IModelMetadataProvider modelMetadataProvider)
            : base(viewOptions, writerFactory, viewEngine, tempDataFactory, diagnosticListener, modelMetadataProvider)
        {
        }

        public StringBuilder StringBuilder { get; } = new StringBuilder();

        public override async Task ExecuteAsync(
            ActionContext actionContext,
            IView view,
            ViewDataDictionary viewData,
            ITempDataDictionary tempData,
            string contentType,
            int? statusCode)
        {
            using (var stringWriter = new StringWriter(StringBuilder))
            {
                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    viewData,
                    tempData,
                    stringWriter,
                    ViewOptions.HtmlHelperOptions);
                await ExecuteAsync(viewContext, contentType, statusCode);
                await stringWriter.FlushAsync();
            }
        }
    }


    private class BenchmarkHostingEnvironment : IWebHostEnvironment
    {
        public BenchmarkHostingEnvironment()
        {
            ApplicationName = typeof(ViewAssemblyMarker).Assembly.FullName;
            WebRootFileProvider = new NullFileProvider();
            ContentRootFileProvider = new NullFileProvider();
            ContentRootPath = AppContext.BaseDirectory;
            WebRootPath = AppContext.BaseDirectory;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }

    protected RuntimePerformanceBenchmarkBase(params string[] viewPaths)
    {
        ViewPaths = viewPaths;
    }

    public virtual string[] ViewPaths { get; private set; }

    [ParamsSource(nameof(ViewPaths))]
    public string ViewPath;

    protected IView View;

    private ServiceProvider _serviceProvider;
    private RouteData _routeData;
    private ActionDescriptor _actionDescriptor;
    private IServiceScope _requestScope;
    private ICompositeViewEngine _viewEngine;
    private BenchmarkViewExecutor _executor;
    private ViewEngineResult _viewEngineResult;
    private ActionContext _actionContext;
    private ViewDataDictionary _viewDataDictionary;
    private ITempDataDictionaryFactory _tempDataDictionaryFactory;
    private ITempDataDictionary _tempData;

    // runs once for every Document value
    [GlobalSetup]
    public void GlobalSetup()
    {
        var loader = new RazorCompiledItemLoader();
        var viewsDll = Path.ChangeExtension(typeof(ViewAssemblyMarker).Assembly.Location, "Views.dll");
        var viewsAssembly = Assembly.Load(File.ReadAllBytes(viewsDll));
        var services = new ServiceCollection();
        var listener = new DiagnosticListener(GetType().Assembly.FullName);
        var partManager = new ApplicationPartManager();
        partManager.ApplicationParts.Add(CompiledRazorAssemblyApplicationPartFactory.GetDefaultApplicationParts(viewsAssembly).Single());
        var builder = services
            .AddSingleton<ILoggerFactory, NullLoggerFactory>()
            .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
            .AddSingleton<DiagnosticSource>(listener)
            .AddSingleton(listener)
            .AddSingleton<IWebHostEnvironment, BenchmarkHostingEnvironment>()
            .AddSingleton<ApplicationPartManager>(partManager)
            .AddScoped<BenchmarkViewExecutor>()
            .AddMvc();

        _serviceProvider = services.BuildServiceProvider();
        _routeData = new RouteData();
        _actionDescriptor = new ActionDescriptor();
        _tempDataDictionaryFactory = _serviceProvider.GetRequiredService<ITempDataDictionaryFactory>();
        _viewEngine = _serviceProvider.GetRequiredService<ICompositeViewEngine>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _serviceProvider.Dispose();
    }

    [IterationSetup]
    public virtual void IterationSetup()
    {
        _requestScope = _serviceProvider.CreateScope();

        _viewEngineResult = _viewEngine.GetView(null, ViewPath, true);
        _viewEngineResult.EnsureSuccessful(null);

        _actionContext = new ActionContext(
            new DefaultHttpContext()
            {
                RequestServices = _requestScope.ServiceProvider
            },
            _routeData,
            _actionDescriptor);

        _tempData = _tempDataDictionaryFactory.GetTempData(_actionContext.HttpContext);

        _viewDataDictionary = new ViewDataDictionary(
            _requestScope.ServiceProvider.GetRequiredService<IModelMetadataProvider>(),
            _actionContext.ModelState);
        _viewDataDictionary.Model = Model;

        _executor = _requestScope.ServiceProvider.GetRequiredService<BenchmarkViewExecutor>();
    }

    [IterationCleanup]
    public virtual void IterationCleanup()
    {
        if (_viewEngineResult.View is IDisposable d)
        {
            d.Dispose();
        }
        _requestScope.Dispose();
    }

    protected virtual object Model { get; } = null;

    [Benchmark]
    public async Task<string> RenderView()
    {
        await _executor.ExecuteAsync(
            _actionContext,
            _viewEngineResult.View,
            _viewDataDictionary,
            _tempData,
            "text/html",
            200);
        return _executor.StringBuilder.ToString();
    }
}
