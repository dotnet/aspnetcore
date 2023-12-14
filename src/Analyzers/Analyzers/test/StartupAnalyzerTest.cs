// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.AspNetCore.Analyzers;

public class StartupAnalyzerTest
{
    public StartupAnalyzerTest()
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
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class StartupSignatures_Standard
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {

        }
    }
}
";
        // Act
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);

        // Assert
        Assert.Collection(ConfigureServicesMethods, m => Assert.Equal("ConfigureServices", m.Name));
        Assert.Collection(ConfigureMethods, m => Assert.Equal("Configure", m.Name));
    }

    [Fact]
    public async Task StartupAnalyzer_FindsStartupMethods_StartupSignatures_MoreVariety()
    {
        // Arrange
        var source = @"
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class StartupSignatures_MoreVariety
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void ConfigureServices(IServiceCollection services, StringBuilder s) // Ignored
        {
        }

        public void Configure(StringBuilder s) // Ignored,
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        public void ConfigureProduction(IWebHostEnvironment env, IApplicationBuilder app)
        {
        }

        private void Configure(IApplicationBuilder app)  // Ignored
        {
        }
    }
}
";

        // Act
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);

        // Assert
        Assert.Collection(
            ConfigureServicesMethods.OrderBy(m => m.Name),
            m => Assert.Equal("ConfigureServices", m.Name));

        Assert.Collection(
            ConfigureMethods.OrderBy(m => m.Name),
            m => Assert.Equal("Configure", m.Name),
            m => Assert.Equal("ConfigureProduction", m.Name));
    }

    [Fact]
    public async Task StartupAnalyzer_MvcOptionsAnalysis_UseMvc_FindsEndpointRoutingDisabled()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcWithDefaultRouteAndEndpointRoutingDisabled
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options => options.EnableEndpointRouting = false);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
        }
    }
}";

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

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcWithDefaultRouteAndAddMvcOptionsEndpointRoutingDisabled
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddMvcOptions(options => options.EnableEndpointRouting = false);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
        }
    }
}
";

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

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvc
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            {|#0:app.UseMvc()|};
        }
    }
}";
        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
            .WithArguments("UseMvc", "ConfigureServices")
            .WithLocation(0);

        return VerifyMvcOptionsAnalysis(source, "UseMvc", diagnosticResult);
    }

    [Fact]
    public Task StartupAnalyzer_MvcOptionsAnalysis_UseMvcAndConfiguredRoutes_FindsEndpointRoutingEnabled()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcAndConfiguredRoutes
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            {|#0:app.UseMvc(routes =>
            {
                routes.MapRoute(""Name"", ""Template"");
            })|};
        }
    }
}
";
        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
            .WithArguments("UseMvc", "ConfigureServices")
            .WithLocation(0);

        return VerifyMvcOptionsAnalysis(source, "UseMvc", diagnosticResult);
    }

    [Fact]
    public Task StartupAnalyzer_MvcOptionsAnalysis_MvcOptions_UseMvcWithDefaultRoute_FindsEndpointRoutingEnabled()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcWithDefaultRoute
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            {|#0:app.UseMvcWithDefaultRoute()|};
        }
    }
}
";
        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
            .WithArguments("UseMvcWithDefaultRoute", "ConfigureServices")
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcWithOtherMiddleware
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseMiddleware<AuthorizationMiddleware>();

            {|#0:app.UseMvc()|};

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
            });
        }
    }
}";
        var diagnosticResult = new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
            .WithLocation(0)
            .WithArguments("UseMvc", "ConfigureServices");

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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcMultiple
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            {|#0:app.UseMvcWithDefaultRoute()|};

            app.UseStaticFiles();
            app.UseMiddleware<AuthorizationMiddleware>();

            {|#1:app.UseMvc()|};

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
            });

            {|#2:app.UseMvc()|};
        }
    }
}";
        var diagnosticResults = new[]
        {
            new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
                .WithLocation(0)
                .WithArguments("UseMvcWithDefaultRoute", "ConfigureServices"),

            new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
                .WithLocation(1)
                .WithArguments("UseMvc", "ConfigureServices"),

            new DiagnosticResult(StartupAnalyzer.Diagnostics.UnsupportedUseMvcWithEndpointRouting)
                .WithLocation(2)
                .WithArguments("UseMvc", "ConfigureServices"),
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

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class ConfigureServices_BuildServiceProvider
    {
        public void ConfigureServices(IServiceCollection services)
        {
            {|#0:services.BuildServiceProvider()|};
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}";

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

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseAuthConfiguredCorrectly
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(r => { });
        }
    }
}";

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

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest {
    public class UseAuthConfiguredCorrectlyChained
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting()
               .UseAuthorization()
               .UseEndpoints(r => { });
        }
    }
}";

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

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseAuthMultipleTimes
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthorization();
            app.UseEndpoints(r => { });
        }
    }
}";

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

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseAuthBeforeUseRouting
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseFileServer();
            {|#0:app.UseAuthorization()|};
            app.UseRouting();
            app.UseEndpoints(r => { });
        }
    }
}";

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

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseAuthBeforeUseRoutingChained
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseFileServer()
               .UseAuthorization()
               .UseRouting()
               .UseEndpoints(r => { });
        }
    }
}";

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

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseAuthAfterUseEndpoints
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(r => { });
            {|#0:app.UseAuthorization()|};
        }
    }
}
";
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

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseAuthFallbackPolicy
    {
        public void Configure(IApplicationBuilder app)
        {
            // This sort of setup would be useful if the user wants to use Auth for non-endpoint content to be handled using the Fallback policy, while
            // using the second instance for regular endpoint routing based auth. We do not want to produce a warning in this case.
            app.UseAuthorization();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(r => { });
        }
    }
}";

        // Act
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);

        // Assert
        var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
        Assert.NotEmpty(middlewareAnalysis.Middleware);
    }

    private Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new StartupCSharpAnalyzerTest(StartupAnalyzer, TestReferences.MetadataReferences)
        {
            TestCode = source,
            ReferenceAssemblies = TestReferences.EmptyReferenceAssemblies,
        };
        // Tests are just the Configure/ConfigureServices methods, no Main, so we need to mark the output as not console
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }
}
