// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;
using Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.Fixers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

public static class CSharpWebApplicationBuilderCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : WebApplicationBuilderAnalyzer, new()
    where TCodeFix : WebApplicationBuilderFixer, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId = null)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic(diagnosticId);

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => new DiagnosticResult(descriptor);

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpWebApplicationBuilderAnalyzerVerifier<TAnalyzer>.Test { TestCode = source };
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public static Task VerifyCodeFixAsync(string source, string fixedSource)
        => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

    public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
        => VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

    public static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
        => VerifyCodeFixAsync(source, expected, fixedSource, string.Empty);

    public static Task VerifyCodeFixAsync(string sources, DiagnosticResult[] expected, string fixedSources, string usageSource = "")
    {
        var test = new WebApplicationBuilderAnalyzerTest
        {
            TestState =
            {
                Sources = { sources, usageSource },
                // We need to set the output type to an exe to properly
                // support top-level programs in the tests. Otherwise,
                // the test infra will assume we are trying to build a library.
                OutputKind = OutputKind.ConsoleApplication
            },
            FixedState =
            {
                Sources =  { fixedSources, usageSource }
            }
        };

        test.TestState.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public class WebApplicationBuilderAnalyzerTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
    {
        public WebApplicationBuilderAnalyzerTest()
        {
            // We populate the ReferenceAssemblies used in the tests with the locally-built AspNetCore
            // assemblies that are referenced in a minimal app to ensure that there are no reference
            // errors during the build. The value used here should be updated on each TFM change.
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70.AddAssemblies(ImmutableArray.Create(
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Hosting.WebHostBuilderExtensions).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.Hosting.IHostBuilder).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.Hosting.HostingHostBuilderExtensions).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.ConfigureHostBuilder).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.ConfigureWebHostBuilder).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Hosting.HostingAbstractionsWebHostBuilderExtensions).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.Logging.ILoggingBuilder).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.Logging.ConsoleLoggerExtensions).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.WebApplication).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.IApplicationBuilder).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.IEndpointConventionBuilder).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.Hosting.IHost).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Mvc.ModelBinding.IBinderTypeProviderMetadata).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.DependencyInjection.AntiforgeryServiceCollectionExtensions).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Mvc.BindAttribute).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.FileProviders.IFileProvider).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.Configuration.ConfigurationManager).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.Configuration.JsonConfigurationExtensions).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.Configuration.IConfigurationBuilder).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.Configuration.EnvironmentVariablesExtensions).Assembly.Location)));

            string TrimAssemblyExtension(string fullPath) => fullPath.Replace(".dll", string.Empty);
        }
    }
}
