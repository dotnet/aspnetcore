// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using WellKnownType = Microsoft.AspNetCore.App.Analyzers.Infrastructure.WellKnownTypeData.WellKnownType;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseCreateHostBuilderInsteadOfCreateWebHostBuilderAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.UseCreateHostBuilderInsteadOfCreateWebHostBuilder
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);

            context.RegisterOperationAction(context =>
            {
                var invocation = (IInvocationOperation)context.Operation;
                var targetMethod = invocation.TargetMethod;

                // Check if this is WebHost.CreateDefaultBuilder
                if (IsWebHostCreateDefaultBuilderCall(targetMethod, wellKnownTypes))
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.UseCreateHostBuilderInsteadOfCreateWebHostBuilder,
                        invocation.Syntax.GetLocation()
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }, OperationKind.Invocation);

            context.RegisterSyntaxNodeAction(context =>
            {
                var methodDeclaration = (MethodDeclarationSyntax)context.Node;
                var semantic = context.SemanticModel;
                var symbol = semantic.GetDeclaredSymbol(methodDeclaration);
                
                // Check if this method returns IWebHostBuilder
                if (symbol != null && IsWebHostBuilderReturnType(symbol, wellKnownTypes))
                {
                    // Check if the method body contains WebHost.CreateDefaultBuilder
                    if (ContainsWebHostCreateDefaultBuilder(methodDeclaration))
                    {
                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.UseCreateHostBuilderInsteadOfCreateWebHostBuilder,
                            methodDeclaration.ReturnType.GetLocation()
                        );
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }, SyntaxKind.MethodDeclaration);
        });
    }

    private static bool IsWebHostCreateDefaultBuilderCall(IMethodSymbol method, WellKnownTypes wellKnownTypes)
    {
        // Check if this is WebHost.CreateDefaultBuilder (not other WebHost methods)
        if (method.Name == "CreateDefaultBuilder" &&
            SymbolEqualityComparer.Default.Equals(method.ContainingType, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_WebHost)))
        {
            return true;
        }

        return false;
    }

    private static bool IsWebHostBuilderReturnType(IMethodSymbol method, WellKnownTypes wellKnownTypes)
    {
        // Check if the return type is IWebHostBuilder
        var returnType = method.ReturnType;
        return SymbolEqualityComparer.Default.Equals(returnType, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Hosting_IWebHostBuilder));
    }

    private static bool ContainsWebHostCreateDefaultBuilder(MethodDeclarationSyntax methodDeclaration)
    {
        // Check if the method contains WebHost.CreateDefaultBuilder calls
        var descendants = methodDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>();
        
        foreach (var invocation in descendants)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.ValueText == "WebHost" &&
                memberAccess.Name.Identifier.ValueText == "CreateDefaultBuilder")
            {
                return true;
            }
        }
        
        return false;
    }
}
