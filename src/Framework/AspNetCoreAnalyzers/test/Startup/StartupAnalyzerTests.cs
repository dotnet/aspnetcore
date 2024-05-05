// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.AspNetCore.Analyzers.Startup;

public sealed class StartupAnalyzerTests
{
    public StartupAnalyzerTests()
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

    [Theory]
    [InlineData("AddControllers")]
    [InlineData("AddControllersWithViews")]
    [InlineData("AddMvc")]
    [InlineData("AddRazorPages")]
    public async Task StartupAnalyzer_ProblemDetailsWriter_AfterMvcServiceCollectionsExtension_ReportsDiagnostic(string methodName)
    {
        // Arrange
        var source = $@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{{
    public class ProblemDetailsWriterRegistration
    {{
        public void ConfigureServices(IServiceCollection services)
        {{
            services.{methodName}();
            {{|#0:services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriter>()|}};
        }}
    }}
    {_sampleProblemDetailsWriterSource}
}}";

        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
            .WithLocation(0);

        // Act + Assert
        await VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Theory]
    [InlineData("AddScoped")]
    [InlineData("AddSingleton")]
    public async Task StartupAnalyzer_ProblemDetailsWriter_OtherLifetimes_AfterMvcServiceCollectionsExtension_ReportsDiagnostic(string methodName)
    {
        // Arrange
        var source = $@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{{
    public class ProblemDetailsWriterRegistration
    {{
        public void ConfigureServices(IServiceCollection services)
        {{
            services.AddControllers();
            {{|#0:services.{methodName}<IProblemDetailsWriter, SampleProblemDetailsWriter>()|}};
        }}
    }}
    {_sampleProblemDetailsWriterSource}
}}";

        var diagnosticResult = new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
            .WithLocation(0);

        // Act + Assert
        await VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Theory]
    [InlineData("AddControllers")]
    [InlineData("AddControllersWithViews")]
    [InlineData("AddMvc")]
    [InlineData("AddRazorPages")]
    public async Task StartupAnalyzer_ProblemDetailsWriter_BeforeMvcServiceCollectionsExtension_ReportsNoDiagnostics(string methodName)
    {
        // Arrange
        var source = $@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{{
    public class ProblemDetailsWriterRegistration
    {{
        public void ConfigureServices(IServiceCollection services)
        {{
            services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriter>();
            services.{methodName}();
        }}
    }}
    {_sampleProblemDetailsWriterSource}
}}";

        // Act
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);

        // Assert
        var middlewareAnalysis = Analyses.OfType<ServicesAnalysis>().First();
        Assert.NotEmpty(middlewareAnalysis.Services);
    }

    private const string _sampleProblemDetailsWriterSource = @"
public class SampleProblemDetailsWriter : IProblemDetailsWriter
{
    public bool CanWrite(ProblemDetailsContext context)
        => context.HttpContext.Response.StatusCode == 400;
    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var response = context.HttpContext.Response;
        return new ValueTask(response.WriteAsJsonAsync(context.ProblemDetails));
    }
}";

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