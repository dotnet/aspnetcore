// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using NuGet.Frameworks;
using Xunit;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public static class CSharpRouteHandlerAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : RouteHandlerAnalyzer, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId = null)
        => CSharpRouteHandlerAnalyzerVerifier<RouteHandlerAnalyzer>.Diagnostic(diagnosticId);

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => new DiagnosticResult(descriptor);

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new Test { TestCode = source };
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public class Test : CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, XUnitVerifier> {
        public Test()
        {
            // We populate the ReferenceAssemblies used in the tests with the locally-built AspNetCore
            // assemblies that are referenced in a minimal app to ensure that there are no reference
            // errors during the build.
            ReferenceAssemblies = Net70.AddAssemblies(ImmutableArray.Create(
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.WebApplication).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.IApplicationBuilder).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Builder.IEndpointConventionBuilder).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.Extensions.Hosting.IHost).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Mvc.ModelBinding.IBinderTypeProviderMetadata).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Mvc.BindAttribute).Assembly.Location),
                TrimAssemblyExtension(typeof(Microsoft.AspNetCore.Http.HttpContext).Assembly.Location)));

            static string TrimAssemblyExtension(string fullPath) => fullPath.Replace(".dll", string.Empty);
        }

        private static ReferenceAssemblies Net70 => _lazyNet70.Value;

        private static readonly Lazy<ReferenceAssemblies> _lazyNet70 =
            new(() =>
            {
                if (!NuGetFramework.Parse("net7.0").IsPackageBased)
                {
                    // The NuGet version provided at runtime does not recognize the 'net6.0' target framework
                    throw new NotSupportedException("The 'net7.0' target framework is not supported by this version of NuGet.");
                }

                return new ReferenceAssemblies(
                    "net7.0",
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref",
                        "7.0.0-alpha.1.21475.3"),
                    Path.Combine("ref", "net7.0"));
            });
    }
}
