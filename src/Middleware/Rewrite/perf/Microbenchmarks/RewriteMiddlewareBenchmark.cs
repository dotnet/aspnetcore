// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite.Microbenchmarks;

[MemoryDiagnoser]
public class RewriteMiddlewareBenchmark
{
    private const string IISRules = """
        <rewrite>
          <rules>
            <rule name="Legacy" stopProcessing="true">
              <match url="^legacy/(.*)$" />
              <action type="Rewrite" url="/new/{R:1}" />
            </rule>
            <rule name="Blog" stopProcessing="true">
              <match url="^blog/(\d+)/(.*)$" />
              <action type="Rewrite" url="/posts/{R:1}?slug={R:2}" />
            </rule>
            <rule name="Docs" stopProcessing="true">
              <match url="^docs/(.*)\.aspx$" />
              <action type="Redirect" url="/docs/{R:1}" />
            </rule>
          </rules>
        </rewrite>
        """;

    private ServiceProvider _services = null!;
    private RequestDelegate _rewriteRules = null!;
    private RequestDelegate _rewriteRuleMatch = null!;
    private RequestDelegate _redirectRules = null!;
    private RequestDelegate _iisRules = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        // Debug logging is off, like a typical production configuration.
        services.AddLogging();
        services.AddSingleton<IWebHostEnvironment>(new BenchmarkWebHostEnvironment());
        _services = services.BuildServiceProvider();

        _rewriteRules = CreatePipeline(new RewriteOptions()
            .AddRewrite("^legacy/(.*)$", "new/$1", skipRemainingRules: true)
            .AddRewrite("^blog/(\\d+)/(.*)$", "posts/$1?slug=$2", skipRemainingRules: true)
            .AddRewrite("^docs/(.*)\\.aspx$", "docs/$1", skipRemainingRules: true));
        _rewriteRuleMatch = CreatePipeline(new RewriteOptions()
            .AddRewrite("^api/products/(\\d+)/(.*)$", "api/v2/products/$1/$2", skipRemainingRules: true));
        _redirectRules = CreatePipeline(new RewriteOptions()
            .AddRedirect("^legacy/(.*)$", "new/$1")
            .AddRedirect("^blog/(\\d+)/(.*)$", "posts/$1?slug=$2")
            .AddRedirect("^docs/(.*)\\.aspx$", "docs/$1"));
        using var reader = new StringReader(IISRules);
        _iisRules = CreatePipeline(new RewriteOptions().AddIISUrlRewrite(reader));
    }

    [GlobalCleanup]
    public void GlobalCleanup() => _services.Dispose();

    [Benchmark(Baseline = true)]
    public HttpContext ContextOnly() => CreateHttpContext();

    [Benchmark]
    public Task RewriteRules_NoMatch() => _rewriteRules(CreateHttpContext());

    [Benchmark]
    public Task RewriteRules_Match() => _rewriteRuleMatch(CreateHttpContext());

    [Benchmark]
    public Task RedirectRules_NoMatch() => _redirectRules(CreateHttpContext());

    [Benchmark]
    public Task IISRules_NoMatch() => _iisRules(CreateHttpContext());

    private RequestDelegate CreatePipeline(RewriteOptions options)
    {
        var app = new ApplicationBuilder(_services);
        app.UseRewriter(options);
        app.Run(_ => Task.CompletedTask);
        return app.Build();
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = HttpMethods.Get;
        request.Scheme = "https";
        request.Host = new HostString("www.example.com");
        request.Path = "/api/products/12345/details";
        return context;
    }

    private sealed class BenchmarkWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "RewriteMiddlewareBenchmark";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
