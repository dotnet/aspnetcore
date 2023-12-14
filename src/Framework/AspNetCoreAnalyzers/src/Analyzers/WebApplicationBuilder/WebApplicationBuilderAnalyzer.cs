// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

using WellKnownType = WellKnownTypeData.WellKnownType;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WebApplicationBuilderAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.DoNotUseConfigureWebHostWithConfigureHostBuilder,
        DiagnosticDescriptors.DoNotUseConfigureWithConfigureWebHostBuilder,
        DiagnosticDescriptors.DoNotUseUseStartupWithConfigureWebHostBuilder,
        DiagnosticDescriptors.DoNotUseHostConfigureLogging,
        DiagnosticDescriptors.DoNotUseHostConfigureServices,
        DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder,
        DiagnosticDescriptors.UseTopLevelRouteRegistrationsInsteadOfUseEndpoints
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);

            INamedTypeSymbol[] configureTypes = { wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Hosting_WebHostBuilderExtensions) };
            INamedTypeSymbol[] configureWebHostTypes = { wellKnownTypes.Get(WellKnownType.Microsoft_Extensions_Hosting_GenericHostWebHostBuilderExtensions) };
            INamedTypeSymbol[] userStartupTypes =
            {
                wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Hosting_HostingAbstractionsWebHostBuilderExtensions),
                wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Hosting_WebHostBuilderExtensions),
            };
            INamedTypeSymbol[] configureLoggingTypes =
            {
                wellKnownTypes.Get(WellKnownType.Microsoft_Extensions_Hosting_HostingHostBuilderExtensions),
                wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Hosting_WebHostBuilderExtensions)
            };
            INamedTypeSymbol[] configureServicesTypes =
            {
                wellKnownTypes.Get(WellKnownType.Microsoft_Extensions_Hosting_HostingHostBuilderExtensions),
                wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureWebHostBuilder)
            };
            INamedTypeSymbol[] configureAppTypes =
            {
                wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureHostBuilder),
                wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureWebHostBuilder),
                wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Hosting_WebHostBuilderExtensions),
                wellKnownTypes.Get(WellKnownType.Microsoft_Extensions_Hosting_HostingHostBuilderExtensions),
            };
            INamedTypeSymbol[] configureHostTypes = { wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureHostBuilder) };
            INamedTypeSymbol[] useEndpointTypes =
            {
                wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_EndpointRoutingApplicationBuilderExtensions),
                wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_WebApplication)
            };

            context.RegisterOperationAction(context =>
            {
                var invocation = (IInvocationOperation)context.Operation;
                var targetMethod = invocation.TargetMethod;

                // var builder = WebApplication.CreateBuilder();
                // builder.Host.ConfigureWebHost(x => {});
                if (IsDisallowedMethod(
                        context,
                        invocation,
                        targetMethod,
                        wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureHostBuilder),
                        "ConfigureWebHost",
                        configureWebHostTypes))
                {
                    context.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DoNotUseConfigureWebHostWithConfigureHostBuilder,
                            invocation));
                }

                // var builder = WebApplication.CreateBuilder();
                // builder.WebHost.Configure(x => {});
                if (IsDisallowedMethod(
                        context,
                        invocation,
                        targetMethod,
                        wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureWebHostBuilder),
                        "Configure",
                        configureTypes))
                {
                    context.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DoNotUseConfigureWithConfigureWebHostBuilder,
                            invocation));
                }

                // var builder = WebApplication.CreateBuilder();
                // builder.WebHost.UseStartup<Startup>();
                if (IsDisallowedMethod(
                        context,
                        invocation,
                        targetMethod,
                        wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureWebHostBuilder),
                        "UseStartup",
                        userStartupTypes))
                {
                    context.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DoNotUseUseStartupWithConfigureWebHostBuilder,
                            invocation));
                }

                //var builder = WebApplication.CreateBuilder(args);
                //builder.Host.ConfigureLogging(x => {})
                if (IsDisallowedMethod(
                        context,
                        invocation,
                        targetMethod,
                        wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureHostBuilder),
                        "ConfigureLogging",
                        configureLoggingTypes))
                {
                    context.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DoNotUseHostConfigureLogging,
                            invocation));
                }

                //var builder = WebApplication.CreateBuilder(args);
                //builder.WebHost.ConfigureLogging(x => {})
                if (IsDisallowedMethod(
                        context,
                        invocation,
                        targetMethod,
                        wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureWebHostBuilder),
                        "ConfigureLogging",
                        configureLoggingTypes))
                {
                    context.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DoNotUseHostConfigureLogging,
                            invocation));
                }

                // var builder = WebApplication.CreateBuilder(args);
                // builder.Host.ConfigureServices(x => {});
                if (IsDisallowedMethod(
                        context,
                        invocation,
                        targetMethod,
                        wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureHostBuilder),
                        "ConfigureServices",
                        configureServicesTypes))
                {
                    context.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DoNotUseHostConfigureServices,
                            invocation));
                }

                // var builder = WebApplication.CreateBuilder(args);
                // builder.WebHost.ConfigureServices(x => {});
                if (IsDisallowedMethod(
                        context,
                        invocation,
                        targetMethod,
                        wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureWebHostBuilder),
                        "ConfigureServices",
                        configureServicesTypes))
                {
                    context.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DoNotUseHostConfigureServices,
                            invocation));
                }

                // var builder = WebApplication.CreateBuilder();
                // builder.WebHost.ConfigureAppConfiguration(builder => {});
                if (IsDisallowedMethod(
                        context,
                        invocation,
                        targetMethod,
                        wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureWebHostBuilder),
                        "ConfigureAppConfiguration",
                        configureAppTypes))
                {
                    context.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder,
                            invocation));
                }

                // var builder = WebApplication.CreateBuilder();
                // builder.Host.ConfigureAppConfiguration(builder => {});
                if (IsDisallowedMethod(
                        context,
                        invocation,
                        targetMethod,
                        wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureHostBuilder),
                        "ConfigureAppConfiguration",
                        configureAppTypes))
                {
                    context.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder,
                            invocation));
                }

                // var builder = WebApplication.CreateBuilder();
                // builder.Host.ConfigureHostConfiguration(builder => {});
                if (IsDisallowedMethod(
                        context,
                        invocation,
                        targetMethod,
                        wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_ConfigureHostBuilder),
                        "ConfigureHostConfiguration",
                        configureHostTypes))
                {
                    context.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder,
                            invocation));
                }

                //var builder = WebApplication.CreateBuilder(args);
                //var app= builder.Build();
                //app.UseRouting();
                //app.UseEndpoints(x => {})
                if (IsDisallowedMethod(
                        context,
                        invocation,
                        targetMethod,
                        wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Builder_WebApplication),
                        "UseEndpoints",
                        useEndpointTypes))
                {
                    context.ReportDiagnostic(
                        CreateDiagnostic(
                            DiagnosticDescriptors.UseTopLevelRouteRegistrationsInsteadOfUseEndpoints,
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

                    return Diagnostic.Create(descriptor, location, methodName);
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

        if (!HasInvocationInBody(disallowedMethodName, invocation))
        {
            return false;
        }

        return true;

        static bool IsDisallowedMethod(
            IMethodSymbol methodSymbol,
            string disallowedMethodName,
            INamedTypeSymbol[] disallowedMethodTypes)
        {
            if (!string.Equals(methodSymbol.Name, disallowedMethodName, StringComparison.Ordinal))
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

    private static bool HasInvocationInBody(string disallowedMethodName, IInvocationOperation invocation)
    {
        if (string.Equals(disallowedMethodName, "UseEndpoints", StringComparison.Ordinal))
        {
            foreach (var argument in invocation.Arguments)
            {
                var arguments = argument?.Syntax as ArgumentSyntax;
                var lambdaExpression = arguments?.Expression as SimpleLambdaExpressionSyntax;

                if (lambdaExpression?.Body is BlockSyntax block)
                {
                    foreach (var statement in block.Statements)
                    {
                        if (statement is ExpressionStatementSyntax expressionStatement && expressionStatement.Expression is InvocationExpressionSyntax)
                        {
                            return true; // Method invocation found, so the diagnostic should be triggered
                        }
                    }
                    return false; // Empty block, no method invocation, so the diagnostic should not be triggered
                }
            }
        }

        return true;
    }
}
