// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    public class MinimalStartupTest : StartupAnalyzerTestBase
    {
        public MinimalStartupTest()
        {
            Runner = new AnalyzersDiagnosticAnalyzerRunner(StartupAnalyzer, OutputKind.ConsoleApplication);
        }

        internal override bool HasConfigure => false;

        internal override AnalyzersDiagnosticAnalyzerRunner Runner { get; }

        [Fact]
        public async Task StartupAnalyzer_AuthNoRouting()
        {
            // Arrange
            var source = TestSource.Read(@"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseAuthorization();
app.Run();");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
            Assert.Single(middlewareAnalysis.Middleware);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task StartupAnalyzer_WorksWithNonImplicitMain()
        {
            // Arrange
            var source = TestSource.Read(@"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddMvc();
        var app = builder.Build();
        app.UseStaticFiles();
        app.UseMiddleware<AuthorizationMiddleware>();
        /*MM*/app.UseMvc();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
        });
        app.Run();
    }
}");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var optionsAnalysis = Assert.Single(Analyses.OfType<OptionsAnalysis>());
            Assert.False(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

            var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());

            Assert.Collection(
                middlewareAnalysis.Middleware,
                item => Assert.Equal("UseStaticFiles", item.UseMethod.Name),
                item => Assert.Equal("UseMiddleware", item.UseMethod.Name),
                item => Assert.Equal("UseMvc", item.UseMethod.Name),
                item => Assert.Equal("UseRouting", item.UseMethod.Name),
                item => Assert.Equal("UseEndpoints", item.UseMethod.Name));

            Assert.Collection(
                diagnostics,
                diagnostic =>
                {
                    Assert.Same(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                    Assert.Contains("inside 'Main", diagnostic.GetMessage());
                });
        }

        [Fact]
        public async Task StartupAnalyzer_WorksWithOtherMethodsInProgram()
        {
            // Arrange
            var source = TestSource.Read(@"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddMvc();
        var app = builder.Build();
        app.UseStaticFiles();
        app.UseMiddleware<AuthorizationMiddleware>();
        /*MM*/app.UseMvc();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
        });
        app.Run();
    }

    private static void MethodA()
    {
    }

    private static void MethodB()
    {
    }
}");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var optionsAnalysis = Assert.Single(Analyses.OfType<OptionsAnalysis>());
            Assert.False(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

            var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());

            Assert.Collection(
                middlewareAnalysis.Middleware,
                item => Assert.Equal("UseStaticFiles", item.UseMethod.Name),
                item => Assert.Equal("UseMiddleware", item.UseMethod.Name),
                item => Assert.Equal("UseMvc", item.UseMethod.Name),
                item => Assert.Equal("UseRouting", item.UseMethod.Name),
                item => Assert.Equal("UseEndpoints", item.UseMethod.Name));

            Assert.Collection(
                diagnostics,
                diagnostic =>
                {
                    Assert.Same(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                    Assert.Contains("inside 'Main", diagnostic.GetMessage());
                });
        }

        internal override TestSource GetSource(string scenario)
        {
            string source = null;
            switch (scenario)
            {
                case "StartupSignatures_Standard": //passes
                    source = @"using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet(""/"", () => ""Hello World!"");
app.Run();";
                    break;
                case "StartupSignatures_MoreVariety": //passes
                    source = @"using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create(args);
app.MapGet(""/"", () => ""Hello World!"");
app.Run();";
                    break;
                case "MvcOptions_UseMvcWithDefaultRouteAndEndpointRoutingDisabled": //passes
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc(options => options.EnableEndpointRouting = false);
var app = builder.Build();
app.UseMvcWithDefaultRoute();
app.Run();";
                    break;
                case "MvcOptions_UseMvcWithDefaultRouteAndAddMvcOptionsEndpointRoutingDisabled": //passes
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc().AddMvcOptions(options => options.EnableEndpointRouting = false);
var app = builder.Build();
app.UseMvcWithDefaultRoute();
app.Run();";
                    break;
                case "MvcOptions_UseMvc": //passes (fails)
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
/*MM*/app.UseMvc();
app.Run();";
                    break;
                case "MvcOptions_UseMvcAndConfiguredRoutes": //passes (fails)
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
/*MM*/app.UseMvc(routes =>
{
    routes.MapRoute(""Name"", ""Template"");
});
app.Run();";
                    break;
                case "MvcOptions_UseMvcWithDefaultRoute": //passes (fails)
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
/*MM*/app.UseMvcWithDefaultRoute();
app.Run();";
                    break;
                case "MvcOptions_UseMvcWithOtherMiddleware": //passes (fails)
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
app.UseStaticFiles();
app.UseMiddleware<AuthorizationMiddleware>();
/*MM*/app.UseMvc();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
});
app.Run();";
                    break;
                case "MvcOptions_UseMvcMultiple": //passes (fails)
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
/*MM1*/app.UseMvcWithDefaultRoute();
app.UseStaticFiles();
app.UseMiddleware<AuthorizationMiddleware>();
/*MM2*/app.UseMvc();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
});
/*MM3*/app.UseMvc();
app.Run();";
                    break;
                case "UseAuthConfiguredCorrectly": //passes
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(r => {});
app.Run();";
                    break;
                case "UseAuthConfiguredCorrectlyChained": //passes
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseRouting()
    .UseAuthorization()
    .UseEndpoints(r => {});
app.Run();";
                    break;
                case "UseAuthMultipleTimes": //passes
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseRouting();
app.UseAuthorization();
app.UseAuthorization();
app.UseEndpoints(r => {});
app.Run();";
                    break;
                case "UseAuthBeforeUseRouting": //passes (fails)
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseFileServer();
/*MM*/app.UseAuthorization();
app.UseRouting();
app.UseEndpoints(r => {});
app.Run();";
                    break;
                case "UseAuthBeforeUseRoutingChained": //passes (fails)
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseFileServer()
    .UseAuthorization()
    .UseRouting()
    .UseEndpoints(r => {});
app.Run();";
                    break;
                case "UseAuthAfterUseEndpoints": //passes (fails)
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseRouting();
app.UseEndpoints(r => { });
/*MM*/app.UseAuthorization();
app.Run();";
                    break;
                case "UseAuthFallbackPolicy": //passes
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseAuthorization();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(r => { });
app.Run();";
                    break;
                case "ConfigureServices_BuildServiceProvider":
                    source = @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
/*MM1*/builder.Services.BuildServiceProvider();
var app = builder.Build();
app.Run();";
                    break;
            }

            return source is not null ? TestSource.Read(source) : null;
        }
    }
}
