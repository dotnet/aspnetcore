// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WebApplicationBuilderAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(new[]
    {
        DiagnosticDescriptors.DoNotUseConfigureWebHostWithConfigureHostBuilder,
        DiagnosticDescriptors.DoNotUseConfigureWithConfigureWebHostBuilder,
        DiagnosticDescriptors.DoNotUseUseStartupWithConfigureWebHostBuilder,
    });

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
        {
            var compilation = compilationStartAnalysisContext.Compilation;
            if (!WellKnownTypes.TryCreate(compilation, out var wellKnownTypes))
            {
                Debug.Fail("One or more types could not be found. This usually means you are bad at spelling C# type names.");
                return;
            }

            INamedTypeSymbol[] configureTypes = { wellKnownTypes.WebHostBuilderExtensions };
            INamedTypeSymbol[] configureWebHostTypes = { wellKnownTypes.GenericHostWebHostBuilderExtensions };
            INamedTypeSymbol[] userStartupTypes =
            {
                wellKnownTypes.HostingAbstractionsWebHostBuilderExtensions,
                wellKnownTypes.WebHostBuilderExtensions,
            };

            compilationStartAnalysisContext.RegisterOperationAction(operationAnalysisContext =>
            {
                var invocation = (IInvocationOperation)operationAnalysisContext.Operation;
                var targetMethod = invocation.TargetMethod;

                // var builder = WebApplication.CreateBuilder();
                // builder.Host.ConfigureWebHost(x => {});
                if (IsDisallowedMethod(
                        operationAnalysisContext,
                        invocation,
                        targetMethod,
                        wellKnownTypes.ConfigureHostBuilder,
                        "ConfigureWebHost",
                        configureWebHostTypes))
                {
                    operationAnalysisContext.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DoNotUseConfigureWebHostWithConfigureHostBuilder,
                            invocation));
                }

                // var builder = WebApplication.CreateBuilder();
                // builder.WebHost.Configure(x => {});
                if (IsDisallowedMethod(
                        operationAnalysisContext,
                        invocation,
                        targetMethod,
                        wellKnownTypes.ConfigureWebHostBuilder,
                        "Configure",
                        configureTypes))
                {
                    operationAnalysisContext.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DoNotUseConfigureWithConfigureWebHostBuilder,
                            invocation));
                }

                // var builder = WebApplication.CreateBuilder();
                // builder.WebHost.UseStartup<Startup>();
                if (IsDisallowedMethod(
                        operationAnalysisContext,
                        invocation,
                        targetMethod,
                        wellKnownTypes.ConfigureWebHostBuilder,
                        "UseStartup",
                        userStartupTypes))
                {
                    operationAnalysisContext.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DoNotUseUseStartupWithConfigureWebHostBuilder,
                            invocation));
                }

                static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, IInvocationOperation operation)
                {
                    // Take the location for the whole invocation operation as a starting point.
                    var location = operation.Syntax.GetLocation();

                    // As we're analyzing an extension method that might be chained off a number of
                    // properties, we need the location to be where the invocation of the targeted
                    // extension method is, not the beginning of the line where the chain begins.
                    // So in the example `foo.bar.Baz(x => {})` we want the span to be for `Baz(x => {})`.
                    // Otherwise the location can contain other unrelated bits of an invocation chain.
                    // Take for example the below block of C#.
                    //
                    // builder.Host
                    //   .ConfigureWebHost(webHostBuilder => { })
                    //   .ConfigureSomethingElse()
                    //   .ConfigureYetAnotherThing(x => x());
                    //
                    // If we did not just select the method name, the location would end up including
                    // the start of the chain and the leading trivia before the method invocation:
                    //
                    // builder.Host
                    //   .ConfigureWebHost(webHostBuilder => { })
                    //
                    // IdentifierNameSyntax finds non-generic methods (e.g. `Foo()`), whereas
                    // GenericNameSyntax finds generic methods (e.g. `Foo<T>()`).
                    var methodName = operation.Syntax
                        .DescendantNodes()
                        .OfType<SimpleNameSyntax>()
                        .Where(node => node is IdentifierNameSyntax || node is GenericNameSyntax)
                        .Where(node => string.Equals(node.Identifier.Value as string, operation.TargetMethod.Name, StringComparison.Ordinal))
                        .FirstOrDefault();

                    if (methodName is not null)
                    {
                        // If we found the method's name, we can truncate the original location
                        // of any leading chain and any trivia to leave the location as the method
                        // invocation and its arguments: `ConfigureWebHost(webHostBuilder => { })`
                        var methodLocation = methodName.GetLocation();

                        var fullSyntaxLength = location.SourceSpan.Length;
                        var chainAndTriviaLength = methodLocation.SourceSpan.Start - location.SourceSpan.Start;

                        var targetSpan = new TextSpan(
                            methodLocation.SourceSpan.Start,
                            fullSyntaxLength - chainAndTriviaLength);

                        location = Location.Create(operation.Syntax.SyntaxTree, targetSpan);
                    }

                    return Diagnostic.Create(descriptor, location);
                }

            }, OperationKind.Invocation);
        });
    }

    private static bool IsDisallowedMethod(
        in OperationAnalysisContext context,
        IInvocationOperation invocation,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol disallowedReceiverType,
        string disallowedMethodName,
        INamedTypeSymbol[] disallowedMethodTypes)
    {
        if (!IsDisallowedMethod(methodSymbol, disallowedMethodName, disallowedMethodTypes))
        {
            return false;
        }

        var receiverType = invocation.GetReceiverType(context.CancellationToken);

        if (!SymbolEqualityComparer.Default.Equals(receiverType, disallowedReceiverType))
        {
            return false;
        }

        return true;

        static bool IsDisallowedMethod(
            IMethodSymbol methodSymbol,
            string disallowedMethodName,
            INamedTypeSymbol[] disallowedMethodTypes)
        {
            if (!string.Equals(methodSymbol?.Name, disallowedMethodName, StringComparison.Ordinal))
            {
                return false;
            }

            var length = disallowedMethodTypes.Length;
            for (var i = 0; i < length; i++)
            {
                var type = disallowedMethodTypes[i];
                if (SymbolEqualityComparer.Default.Equals(type, methodSymbol.ContainingType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
