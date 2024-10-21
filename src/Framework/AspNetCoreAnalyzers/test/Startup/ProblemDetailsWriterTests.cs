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
        StartupAnalyzer.ServicesAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
        StartupAnalyzer.OptionsAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
        StartupAnalyzer.MiddlewareAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
    }

    internal StartupAnalyzer StartupAnalyzer { get; }

    internal ConcurrentBag<object> Analyses { get; }

    [Theory]
    [InlineData("AddControllers")]
    [InlineData("AddControllersWithViews")]
    [InlineData("AddMvc")]
    [InlineData("AddRazorPages")]
    public async Task StartupAnalyzer_ProblemDetailsWriter_AfterChainedMvcServiceCollectionsExtension_ReportsDiagnostic(string methodName)
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
            {{|#0:services.{methodName}()|}}
                .AddViewLocalization();
            {{|#1:services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriter>()|}};
        }}
    }}
    {GetSampleProblemDetailsWriterSource()}
}}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
            .WithLocation(1)
            .WithLocation(0);

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
            services.{methodName}()
                .AddViewLocalization();
        }}
    }}
    {GetSampleProblemDetailsWriterSource()}
}}";

        // Act + Assert
        await VerifyCodeFix(source, [diagnostic], fixedSource);
    }

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
            {{|#0:services.{methodName}()|}};
            {{|#1:services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriter>()|}};
        }}
    }}
    {GetSampleProblemDetailsWriterSource()}
}}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
            .WithLocation(1)
            .WithLocation(0);

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
    {GetSampleProblemDetailsWriterSource()}
}}";

        // Act + Assert
        await VerifyCodeFix(source, [diagnostic], fixedSource);
    }

    [Theory]
    [InlineData("AddControllers")]
    [InlineData("AddControllersWithViews")]
    [InlineData("AddMvc")]
    [InlineData("AddRazorPages")]
    public async Task StartupAnalyzer_MultipleProblemDetailsWriters_AfterMvcServiceCollectionsExtension_ReportsDiagnostic(string methodName)
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
            {{|#0:services.{methodName}()|}};
            {{|#1:services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriterA>()|}};
            {{|#2:services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriterB>()|}};
            {{|#3:services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriterC>()|}};
        }}
    }}
    {GetSampleProblemDetailsWriterSource("A")}
    {GetSampleProblemDetailsWriterSource("B")}
    {GetSampleProblemDetailsWriterSource("C")}
}}";

        var diagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
                .WithLocation(1)
                .WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
                .WithLocation(2)
                .WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
                .WithLocation(3)
                .WithLocation(0)
        };

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
            services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriterA>();
            services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriterB>();
            services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriterC>();
            services.{methodName}();
        }}
    }}
    {GetSampleProblemDetailsWriterSource("A")}
    {GetSampleProblemDetailsWriterSource("B")}
    {GetSampleProblemDetailsWriterSource("C")}
}}";

        // Act + Assert
        await VerifyCodeFixAll(source, diagnostics, fixedSource);
    }

    [Theory]
    [InlineData("AddControllers")]
    [InlineData("AddControllersWithViews")]
    [InlineData("AddMvc")]
    [InlineData("AddRazorPages")]
    public async Task StartupAnalyzer_ProblemDetailsWriterRegistrationChained_AfterMvcServiceCollectionsExtension_ReportsDiagnosticButDoesNotFix(string methodName)
    {
        // Arrange
        var source = $@"
using System;
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
            {{|#0:services.{methodName}()|}};

            {{|#1:services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriterA>()|}}
                .AddTransient<string[]>(provider => Array.Empty<string>());

            {{|#2:services.AddTransient<string[]>(provider => Array.Empty<string>())
                .AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriterB>()|}};

            {{|#3:services.AddTransient<string[]>(provider => Array.Empty<string>())
                .AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriterB>()|}}
                .AddTransient<string[]>(provider => Array.Empty<string>());  
        }}
    }}
    {GetSampleProblemDetailsWriterSource("A")}
    {GetSampleProblemDetailsWriterSource("B")}
    {GetSampleProblemDetailsWriterSource("C")}
}}";

        var diagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
                .WithLocation(1)
                .WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
                .WithLocation(2)
                .WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
                .WithLocation(3)
                .WithLocation(0)
        };

        var fixedSource = $@"
using System;
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
            {{|#0:services.{methodName}()|}};

            {{|#1:services.AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriterA>()|}}
                .AddTransient<string[]>(provider => Array.Empty<string>());

            {{|#2:services.AddTransient<string[]>(provider => Array.Empty<string>())
                .AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriterB>()|}};

            {{|#3:services.AddTransient<string[]>(provider => Array.Empty<string>())
                .AddTransient<IProblemDetailsWriter, SampleProblemDetailsWriterB>()|}}
                .AddTransient<string[]>(provider => Array.Empty<string>());  
        }}
    }}
    {GetSampleProblemDetailsWriterSource("A")}
    {GetSampleProblemDetailsWriterSource("B")}
    {GetSampleProblemDetailsWriterSource("C")}
}}";

        // Act + Assert
        await VerifyCodeFix(source, diagnostics, fixedSource);
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
            {{|#0:services.AddControllers()|}};
            {{|#1:services.{methodName}<IProblemDetailsWriter, SampleProblemDetailsWriter>()|}};
        }}
    }}
    {GetSampleProblemDetailsWriterSource()}
}}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter)
            .WithLocation(1)
            .WithLocation(0);

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
    {GetSampleProblemDetailsWriterSource()}
}}";

        // Act + Assert
        await VerifyCodeFix(source, [diagnostic], fixedSource);
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
    {GetSampleProblemDetailsWriterSource()}
}}";

        // Act
        await VerifyNoCodeFix(source);

        // Assert
        var middlewareAnalysis = Analyses.OfType<ServicesAnalysis>().First();
        Assert.NotEmpty(middlewareAnalysis.Services);
    }

    private static string GetSampleProblemDetailsWriterSource(string suffix = null)
    {
        return $@"
public class SampleProblemDetailsWriter{suffix} : IProblemDetailsWriter
{{
    public bool CanWrite(ProblemDetailsContext context)
        => context.HttpContext.Response.StatusCode == 400;

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {{
        var response = context.HttpContext.Response;
        return new ValueTask(response.WriteAsJsonAsync(context.ProblemDetails));
    }}
}}";
    }

    private async Task VerifyNoCodeFix(string source)
    {
        await VerifyCodeFix(source, [], source);
    }

    private async Task VerifyCodeFix(string source, DiagnosticResult[] diagnostics, string fixedSource)
    {
        var test = CreateAnalyzerTest();

        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ExpectedDiagnostics.AddRange(diagnostics);

        await test.RunAsync();
    }

    private async Task VerifyCodeFixAll(string source, DiagnosticResult[] diagnostics, string fixedSource)
    {
        var test = CreateAnalyzerTest();

        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ExpectedDiagnostics.AddRange(diagnostics);
        test.NumberOfFixAllIterations = 1;

        await test.RunAsync();
    }

    private StartupCSharpAnalyzerTest<IncorrectlyConfiguredProblemDetailsWriterFixer> CreateAnalyzerTest()
    {
        var test = new StartupCSharpAnalyzerTest<IncorrectlyConfiguredProblemDetailsWriterFixer>(StartupAnalyzer, TestReferences.MetadataReferences)
        {
            ReferenceAssemblies = TestReferences.EmptyReferenceAssemblies
        };

        // Tests are just the Configure/ConfigureServices methods, no Main, so we need to mark the output as not console.
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;

        return test;
    }
}
