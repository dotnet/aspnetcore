// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Analyzers.Startup.Fixers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.AspNetCore.Analyzers.Startup;

public sealed class ProblemDetailsWriterTests
{
    public ProblemDetailsWriterTests()
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
            {{|#1:services.{methodName}()|}};
            {{|#0:services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriter>()|}};
        }}
    }}
    {_sampleProblemDetailsWriterSource}
}}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
            .WithLocation(0)
            .WithLocation(1);

        var fixedSource = $@"
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

        // Act + Assert
        await VerifyCodeFix(source, fixedSource, diagnostic);
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
            {{|#1:services.AddControllers()|}};
            {{|#0:services.{methodName}<IProblemDetailsWriter, SampleProblemDetailsWriter>()|}};
        }}
    }}
    {_sampleProblemDetailsWriterSource}
}}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
            .WithLocation(0)
            .WithLocation(1);

        var fixedSource = $@"
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
            services.{methodName}<IProblemDetailsWriter, SampleProblemDetailsWriter>();
            services.AddControllers();
        }}
    }}
    {_sampleProblemDetailsWriterSource}
}}";

        // Act + Assert
        await VerifyCodeFix(source, fixedSource, diagnostic);
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
        await VerifyNoCodeFix(source);

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

    private Task VerifyNoCodeFix(string source)
    {
        return VerifyCodeFix(source, source, Array.Empty<DiagnosticResult>());
    }

    private async Task VerifyCodeFix(string source, string fixedSource, params DiagnosticResult[] expected)
    {
        var test = new StartupCSharpAnalyzerTest<IncorrectlyConfiguredProblemDetailsWriterFixer>(StartupAnalyzer, TestReferences.MetadataReferences)
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = TestReferences.EmptyReferenceAssemblies,
        };

        // Tests are just the Configure/ConfigureServices methods, no Main, so we need to mark the output as not console
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync();
    }
}
