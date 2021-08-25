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
    public class MinimalStartupTest : AnalyzerTestBase
    {
        public MinimalStartupTest()
        {
            StartupAnalyzer = new StartupAnalyzer();

            Runner = new AnalyzersDiagnosticAnalyzerRunner(StartupAnalyzer, OutputKind.ConsoleApplication);

            Analyses = new ConcurrentBag<object>();
            ConfigureServicesMethods = new ConcurrentBag<IMethodSymbol>();
            ConfigureMethods = new ConcurrentBag<IMethodSymbol>();
            StartupAnalyzer.ServicesAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
            StartupAnalyzer.OptionsAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
            StartupAnalyzer.MiddlewareAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
            StartupAnalyzer.ConfigureServicesMethodFound += (sender, method) => ConfigureServicesMethods.Add(method);
            StartupAnalyzer.ConfigureMethodFound += (sender, method) => ConfigureMethods.Add(method);
        }

        private StartupAnalyzer StartupAnalyzer { get; }

        private AnalyzersDiagnosticAnalyzerRunner Runner { get; }

        private ConcurrentBag<object> Analyses { get; }

        private ConcurrentBag<IMethodSymbol> ConfigureServicesMethods { get; }

        private ConcurrentBag<IMethodSymbol> ConfigureMethods { get; }

        [Fact]
        public async Task StartupAnalyzer_Simple_WebApplicationBuilder()
        {
            // Arrange
            var source = @"using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet(""/"", () => ""Hello World!"");
app.Run();";

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task StartupAnalyzer_UseAuthAfterUseEndpoints_WebApplicationBuilder()
        {
            // Arrange
            var source = TestSource.Read(@"using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseRouting();
app.UseEndpoints(r => { });
/*MM*/app.UseAuthorization();
app.Run();");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
            Assert.NotEmpty(middlewareAnalysis.Middleware);
            Assert.Collection(diagnostics,
                diagnostic =>
                {
                    Assert.Same(StartupAnalyzer.Diagnostics.IncorrectlyConfiguredAuthorizationMiddleware, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                });
        }

        [Theory]
        [InlineData("MvcOptions_UseMvc", "UseMvc")]
        [InlineData("MvcOptions_UseMvcAndConfiguredRoutes", "UseMvc")]
        [InlineData("MvcOptions_UseMvcWithDefaultRoute", "UseMvcWithDefaultRoute")]
        public async Task StartupAnalyzer_UseMvc_WebApplicationBuilder(string sources, string middlewareName)
        {
            // Arrange
            var source = TestSource.Read(MvcSources[sources]);

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
            var middleware = Assert.Single(middlewareAnalysis.Middleware);
            Assert.Equal(middlewareName, middleware.UseMethod.Name);

            Assert.NotEmpty(middlewareAnalysis.Middleware);
            Assert.Collection(diagnostics,
                diagnostic =>
                {
                    Assert.Same(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                });
        }

        private Dictionary<string, string> MvcSources = new()
        {
            { "MvcOptions_UseMvc", @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
/*MM*/app.UseMvc();
app.Run();" },
            { "MvcOptions_UseMvcAndConfiguredRoutes",
                @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
/*MM*/app.UseMvc(routes =>
{
    routes.MapRoute(""Name"", ""Template"");
});
app.Run();" },
            { "MvcOptions_UseMvcWithDefaultRoute", @"using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
/*MM*/app.UseMvcWithDefaultRoute();
app.Run();" }
        };
    }
}
