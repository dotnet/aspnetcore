// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Analyzers.Verifiers;

public static partial class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic()"/>
    public static DiagnosticResult Diagnostic()
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic();

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic(string)"/>
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)"/>
    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor);

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
    public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source.ReplaceLineEndings(),
            // We need to set the output type to an exe to properly
            // support top-level programs in the tests. Otherwise,
            // the test infra will assume we are trying to build a library.
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            ReferenceAssemblies = GetReferenceAssemblies(),
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    internal static ReferenceAssemblies GetReferenceAssemblies()
    {
        var nugetConfigPath = SkipOnHelixAttribute.OnHelix() ?
            Path.Combine(
                Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"),
                "NuGet.config") :
            Path.Combine(TestData.GetRepoRoot(), "NuGet.config");
        var net10Ref = new ReferenceAssemblies(
            "net10.0",
            new PackageIdentity(
                "Microsoft.NETCore.App.Ref",
                TestData.GetMicrosoftNETCoreAppRefPackageVersion()),
            Path.Combine("ref", "net10.0"))
        .WithNuGetConfigFilePath(nugetConfigPath);

        return net10Ref.AddAssemblies(ImmutableArray.Create(
            TrimAssemblyExtension(typeof(System.IO.Pipelines.PipeReader).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServer).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Authorization.IAuthorizeData).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Mvc.ModelBinding.IBinderTypeProviderMetadata).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Mvc.BindAttribute).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Hosting.WebHostBuilderExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Hosting.WebHostBuilderKestrelExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.Hosting.IHostBuilder).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.Hosting.HostingHostBuilderExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.ConfigureHostBuilder).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.ConfigureWebHostBuilder).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.EndpointRoutingApplicationBuilderExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.RateLimiterEndpointConventionBuilderExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.CorsEndpointConventionBuilderExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.DependencyInjection.OutputCacheConventionBuilderExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.AuthorizationEndpointConventionBuilderExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Routing.RouteData).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Components.ComponentBase).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Components.ParameterAttribute).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Http.IResult).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Http.IHeaderDictionary).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Http.HeaderDictionary).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Http.HttpRequestJsonExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Hosting.HostingAbstractionsWebHostBuilderExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.Primitives.StringValues).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.Logging.ILoggingBuilder).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.Logging.ConsoleLoggerExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.DependencyInjection.AntiforgeryServiceCollectionExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.FileProviders.IFileProvider).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.Configuration.ConfigurationManager).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.Configuration.JsonConfigurationExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.Configuration.IConfigurationBuilder).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.Configuration.EnvironmentVariablesExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.Extensions.DependencyInjection.AuthenticationServiceCollectionExtensions).Assembly.Location),
            TrimAssemblyExtension(typeof(Microsoft.JSInterop.IJSRuntime).Assembly.Location)));
    }

    static string TrimAssemblyExtension(string fullPath) => fullPath.Replace(".dll", string.Empty);
}
