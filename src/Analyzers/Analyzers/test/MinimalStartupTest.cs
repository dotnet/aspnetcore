// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.AspNetCore.Analyzers;

public class MinimalStartupTest
{
    private const string TopLevelMainName = "<Main>$";

    public MinimalStartupTest()
    {
        StartupAnalyzer = new StartupAnalyzer();

        Analyses = new ConcurrentBag<object>();
        ConfigureServicesMethods = new ConcurrentBag<IMethodSymbol>();
        ConfigureMethods = new ConcurrentBag<IMethodSymbol>();
        StartupAnalyzer.ServicesAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
        StartupAnalyzer.OptionsAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
        StartupAnalyzer.MiddlewareAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
        StartupAnalyzer.ConfigureServicesMethodFound += (sender, method) => ConfigureServicesMethods.Add(method);
        StartupAnalyzer.ConfigureMethodFound += (sender, method) => ConfigureMethods.Add(method);
    }

    internal StartupAnalyzer StartupAnalyzer { get; }

    internal ConcurrentBag<object> Analyses { get; }

    internal ConcurrentBag<IMethodSymbol> ConfigureServicesMethods { get; }

    internal ConcurrentBag<IMethodSymbol> ConfigureMethods { get; }

    [Fact]
    public async Task StartupAnalyzer_FindsStartupMethods_StartupSignatures_Standard()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet(""/"", () => ""Hello World!"");
app.Run();";

        // Act & Assert
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public async Task StartupAnalyzer_FindsStartupMethods_StartupSignatures_MoreVariety()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create(args);
app.MapGet(""/"", () => ""Hello World!"");
app.Run();";

        // Act & Assert
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public async Task StartupAnalyzer_MvcOptionsAnalysis_UseMvc_FindsEndpointRoutingDisabled()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc(options => options.EnableEndpointRouting = false);
var app = builder.Build();
app.UseMvcWithDefaultRoute();
app.Run();";

        // Act
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);

        // Assert
        var optionsAnalysis = Assert.Single(Analyses.OfType<OptionsAnalysis>());
        Assert.True(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

        var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
        var middleware = Assert.Single(middlewareAnalysis.Middleware);
        Assert.Equal("UseMvcWithDefaultRoute", middleware.UseMethod.Name);
    }

    [Fact]
    public async Task StartupAnalyzer_MvcOptionsAnalysis_AddMvcOptions_FindsEndpointRoutingDisabled()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc().AddMvcOptions(options => options.EnableEndpointRouting = false);
var app = builder.Build();
app.UseMvcWithDefaultRoute();
app.Run();";

        // Act
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);

        // Assert
        var optionsAnalysis = Assert.Single(Analyses.OfType<OptionsAnalysis>());
        Assert.True(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

        var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
        var middleware = Assert.Single(middlewareAnalysis.Middleware);
        Assert.Equal("UseMvcWithDefaultRoute", middleware.UseMethod.Name);
    }

    [Fact]
    public Task StartupAnalyzer_MvcOptionsAnalysis_UseMvc_FindsEndpointRoutingEnabled()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
{|#0:app.UseMvc()|};
app.Run();";
        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
            .WithArguments("UseMvc", TopLevelMainName)
            .WithLocation(0);

        return VerifyMvcOptionsAnalysis(source, "UseMvc", diagnosticResult);
    }

    [Fact]
    public Task StartupAnalyzer_MvcOptionsAnalysis_UseMvcAndConfiguredRoutes_FindsEndpointRoutingEnabled()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
{|#0:app.UseMvc(routes =>
{
    routes.MapRoute(""Name"", ""Template"");
})|};
app.Run();";
        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
            .WithArguments("UseMvc", TopLevelMainName)
            .WithLocation(0);

        return VerifyMvcOptionsAnalysis(source, "UseMvc", diagnosticResult);
    }

    [Fact]
    public Task StartupAnalyzer_MvcOptionsAnalysis_MvcOptions_UseMvcWithDefaultRoute_FindsEndpointRoutingEnabled()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
{|#0:app.UseMvcWithDefaultRoute()|};
app.Run();";

        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
            .WithArguments("UseMvcWithDefaultRoute", TopLevelMainName)
            .WithLocation(0);

        return VerifyMvcOptionsAnalysis(source, "UseMvcWithDefaultRoute", diagnosticResult);
    }

    private async Task VerifyMvcOptionsAnalysis(string source, string mvcMiddlewareName, params DiagnosticResult[] diagnosticResults)
    {
        // Arrange
        await VerifyAnalyzerAsync(source, diagnosticResults);

        // Assert
        var optionsAnalysis = Analyses.OfType<OptionsAnalysis>().First();
        Assert.False(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

        var middlewareAnalysis = Analyses.OfType<MiddlewareAnalysis>().First();
        var middleware = Assert.Single(middlewareAnalysis.Middleware);
        Assert.Equal(mvcMiddlewareName, middleware.UseMethod.Name);
    }

    [Fact]
    public async Task StartupAnalyzer_MvcOptionsAnalysis_MultipleMiddleware()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
app.UseStaticFiles();
app.UseMiddleware<AuthorizationMiddleware>();
{|#0:app.UseMvc()|};
app.UseRouting();
app.UseEndpoints(endpoints =>
{
});
app.Run();";
        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
            .WithLocation(0)
            .WithArguments("UseMvc", TopLevelMainName);

        // Act
        await VerifyAnalyzerAsync(source, diagnosticResult);

        // Assert
        var optionsAnalysis = Analyses.OfType<OptionsAnalysis>().First();
        Assert.False(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

        var middlewareAnalysis = Analyses.OfType<MiddlewareAnalysis>().First();

        Assert.Collection(
            middlewareAnalysis.Middleware,
            item => Assert.Equal("UseStaticFiles", item.UseMethod.Name),
            item => Assert.Equal("UseMiddleware", item.UseMethod.Name),
            item => Assert.Equal("UseMvc", item.UseMethod.Name),
            item => Assert.Equal("UseRouting", item.UseMethod.Name),
            item => Assert.Equal("UseEndpoints", item.UseMethod.Name));
    }

    [Fact]
    public async Task StartupAnalyzer_MvcOptionsAnalysis_MultipleUseMvc()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
var app = builder.Build();
{|#0:app.UseMvcWithDefaultRoute()|};
app.UseStaticFiles();
app.UseMiddleware<AuthorizationMiddleware>();
{|#1:app.UseMvc()|};
app.UseRouting();
app.UseEndpoints(endpoints =>
{
});
{|#2:app.UseMvc()|};
app.Run();";
        var diagnosticResults = new[]
        {
            new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
                .WithLocation(0)
                .WithArguments("UseMvcWithDefaultRoute", TopLevelMainName),

            new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
                .WithLocation(1)
                .WithArguments("UseMvc", TopLevelMainName),

            new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
                .WithLocation(2)
                .WithArguments("UseMvc", TopLevelMainName),
        };

        // Act
        await VerifyAnalyzerAsync(source, diagnosticResults);

        // Assert
        var optionsAnalysis = Analyses.OfType<OptionsAnalysis>().First();
        Assert.False(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));
    }

    [Fact]
    public async Task StartupAnalyzer_ServicesAnalysis_CallBuildServiceProvider()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
{|#0:builder.Services.BuildServiceProvider()|};
var app = builder.Build();
app.Run();";

        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.BuildServiceProviderShouldNotCalledInConfigureServicesMethod)
            .WithLocation(0);

        // Act
        await VerifyAnalyzerAsync(source, diagnosticResult);

        // Assert
        var servicesAnalysis = Analyses.OfType<ServicesAnalysis>().First();
        Assert.NotEmpty(servicesAnalysis.Services);
    }

    [Fact]
    public async Task StartupAnalyzer_UseAuthorizationConfiguredCorrectly_ReportsNoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(r => {});
app.Run();";

        // Act
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);

        // Assert
        var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
        Assert.NotEmpty(middlewareAnalysis.Middleware);
    }

    [Fact]
    public async Task StartupAnalyzer_UseAuthorizationConfiguredAsAChain_ReportsNoDiagnostics()
    {
        // Regression test for https://github.com/dotnet/aspnetcore/issues/15203
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseRouting()
    .UseAuthorization()
    .UseEndpoints(r => {});
app.Run();";

        // Act
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);

        // Assert
        var middlewareAnalysis = Analyses.OfType<MiddlewareAnalysis>().First();
        Assert.NotEmpty(middlewareAnalysis.Middleware);
    }

    [Fact]
    public async Task StartupAnalyzer_UseAuthorizationInvokedMultipleTimesInEndpointRoutingBlock_ReportsNoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
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

        // Act
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);

        // Assert
        var middlewareAnalysis = Analyses.OfType<MiddlewareAnalysis>().First();
        Assert.NotEmpty(middlewareAnalysis.Middleware);
    }

    [Fact]
    public async Task StartupAnalyzer_UseAuthorizationConfiguredBeforeUseRouting_ReportsDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseFileServer();
{|#0:app.UseAuthorization()|};
app.UseRouting();
app.UseEndpoints(r => {});
app.Run();";

        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.IncorrectlyConfiguredAuthorizationMiddleware)
            .WithLocation(0);

        // Act
        await VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public async Task StartupAnalyzer_UseAuthorizationConfiguredBeforeUseRoutingChained_ReportsDiagnostics()
    {
        // This one asserts a false negative for https://github.com/dotnet/aspnetcore/issues/15203.
        // We don't correctly identify chained calls, this test verifies the behavior.
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
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

        // Act
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);

        // Assert
        var middlewareAnalysis = Analyses.OfType<MiddlewareAnalysis>().First();
        Assert.NotEmpty(middlewareAnalysis.Middleware);
    }

    [Fact]
    public async Task StartupAnalyzer_UseAuthorizationConfiguredAfterUseEndpoints_ReportsDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseRouting();
app.UseEndpoints(r => { });
{|#0:app.UseAuthorization()|};
app.Run();";

        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.IncorrectlyConfiguredAuthorizationMiddleware)
            .WithLocation(0);

        // Act
        await VerifyAnalyzerAsync(source, diagnosticResult);

        // Assert
        var middlewareAnalysis = Analyses.OfType<MiddlewareAnalysis>().First();
        Assert.NotEmpty(middlewareAnalysis.Middleware);
    }

    [Fact]
    public async Task StartupAnalyzer_MultipleUseAuthorization_ReportsNoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
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

        // Act
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);

        // Assert
        var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
        Assert.NotEmpty(middlewareAnalysis.Middleware);
    }

    protected Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new StartupCSharpAnalyzerTest(StartupAnalyzer, TestReferences.MetadataReferences)
        {
            TestCode = source,
            ReferenceAssemblies = TestReferences.EmptyReferenceAssemblies,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    [Fact]
    public async Task StartupAnalyzer_AuthNoRouting()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseAuthorization();
app.Run();";

        // Act
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);

        // Assert
        var middlewareAnalysis = Analyses.OfType<MiddlewareAnalysis>().First();
        Assert.Single(middlewareAnalysis.Middleware);
    }

    [Fact]
    public async Task StartupAnalyzer_WorksWithNonImplicitMain()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
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
        {|#0:app.UseMvc()|};
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
        });
        app.Run();
    }
}";
        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
            .WithLocation(0)
            .WithArguments("UseMvc", "Main");

        // Act
        await VerifyAnalyzerAsync(source, diagnosticResult);

        // Assert
        var optionsAnalysis = Analyses.OfType<OptionsAnalysis>().First();
        Assert.False(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

        var middlewareAnalysis = Analyses.OfType<MiddlewareAnalysis>().First();

        Assert.Collection(
            middlewareAnalysis.Middleware,
            item => Assert.Equal("UseStaticFiles", item.UseMethod.Name),
            item => Assert.Equal("UseMiddleware", item.UseMethod.Name),
            item => Assert.Equal("UseMvc", item.UseMethod.Name),
            item => Assert.Equal("UseRouting", item.UseMethod.Name),
            item => Assert.Equal("UseEndpoints", item.UseMethod.Name));
    }

    [Fact]
    public async Task StartupAnalyzer_WorksWithOtherMethodsInProgram()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
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
        {|#0:app.UseMvc()|};
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
}";
        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
            .WithLocation(0)
            .WithArguments("UseMvc", "Main");

        // Act
        await VerifyAnalyzerAsync(source, diagnosticResult);

        // Assert
        var optionsAnalysis = Analyses.OfType<OptionsAnalysis>().First();
        Assert.False(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

        var middlewareAnalysis = Analyses.OfType<MiddlewareAnalysis>().First();

        Assert.Collection(
            middlewareAnalysis.Middleware,
            item => Assert.Equal("UseStaticFiles", item.UseMethod.Name),
            item => Assert.Equal("UseMiddleware", item.UseMethod.Name),
            item => Assert.Equal("UseMvc", item.UseMethod.Name),
            item => Assert.Equal("UseRouting", item.UseMethod.Name),
            item => Assert.Equal("UseEndpoints", item.UseMethod.Name));
    }
}
